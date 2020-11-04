using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace GitHubReleaser.Model
{
  internal class Releaser
  {
    public ReleaserSettings Settings { get; set; }
    private readonly GitHubClient _client;
    private readonly string ACCOUNT;
    private readonly string REPO;
    private readonly string VersionMilestone;
    private string VersionFull;

    public Releaser(ReleaserSettings releaserSettings)
    {
      Settings = releaserSettings;
      var version = Assembly.LoadFile(Settings.FileForVersion).GetName().Version;
      VersionMilestone = version.ToString(3);
      VersionFull = version.ToString();

      var split = Settings.GitHubRepo.Split('/');
      ACCOUNT = split.First();
      REPO = split.Last();

      // GitHub
      ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12; // needed https://github.com/octokit/octokit.net/issues/1756
      var connection = new Connection(new ProductHeaderValue(REPO));
      _client = new GitHubClient(connection);
      var tokenAuth = new Credentials(Settings.GitHubToken);
      _client.Credentials = tokenAuth;
    }

    public async Task ExecuteAsync()
    {
      if (Settings.IsUpdateOnly)
      {
        // todo
      }
      else
      {
        var release = await CreateRelease();
      }

      if (Settings.IsChangelogFileCreationEnabled)
      {
        // todo
      }
    }

    private async Task<Release> CreateRelease()
    {
      // Remove existing
      Release release = await _client.Repository.Release.Get(ACCOUNT, REPO, VersionFull);
      if (release != null)
      {
        Log.Information("Remove release...");
        await _client.Repository.Release.Delete(ACCOUNT, REPO, release.Id);
      }

      // Create
      Log.Information("Create release...");
      NewRelease newRelease = new NewRelease(VersionFull);
      newRelease.Name = VersionFull;
      newRelease.Prerelease = Settings.IsPreRelease;
      newRelease.Draft = Settings.IsDraft;

      // Changelog
      var changelog = await GetReleaseChangelog();
      if (newRelease.Prerelease)
      {
        string alert = "| :warning: **PreRelease**: Bitte nur in Testumgebung nutzen! |" +
                       Environment.NewLine +
                       "| --- |";
        changelog = alert + Environment.NewLine + changelog;
      }
      newRelease.Body = changelog;

      release = await _client.Repository.Release.Create(ACCOUNT, REPO, newRelease);

      // Upload: Attachments
      Log.Information("Upload attachments...");
      bool deleteFilesAfterUpload = true;
      try
      {
        for (var index = 0; index < Settings.ReleaseAttachments.Count; index++)
        {
          Log.Information(index + 1 + " / " + Settings.ReleaseAttachments.Count);

          var setupFile = Settings.ReleaseAttachments[index];
          await using var archiveContents = File.OpenRead(setupFile);
          string assetFilename = Path.GetFileName(setupFile);
          var assetUpload = new ReleaseAssetUpload
          {
            FileName = assetFilename,
            ContentType = "application/x-msdownload",
            RawData = archiveContents
          };
          await _client.Repository.Release.UploadAsset(release, assetUpload);
        }
      }
      catch (Exception exception)
      {
        Log.Error(exception.ToString());
        deleteFilesAfterUpload = false;
      }

      if (deleteFilesAfterUpload)
      {
        foreach (var attachment in Settings.ReleaseAttachments)
        {
          Directory.Delete(attachment, true);
        }
      }

      return release;
    }

    private async Task<string> GetReleaseChangelog()
    {
      var releases = await _client.Repository.Release.GetAll(ACCOUNT, REPO);
      Release lastRelease = releases.OrderBy(obj => obj.CreatedAt.DateTime).Last();
      IssueRequest recently = new IssueRequest();
      recently.Filter = IssueFilter.All;
      recently.State = ItemStateFilter.Closed;

      if (Settings.IsPreRelease)
      {
        var lastReleaseCreatedDate = lastRelease.PublishedAt;
        recently.Since = lastReleaseCreatedDate;
      }

      var allIssues = await _client.Issue.GetAllForCurrent(recently);
      var issuesWithLabel = new List<IssueWithLabel>();
      foreach (var issue in allIssues)
      {
        if (issue.Milestone == null)
        {
          continue;
        }

        if (!issue.Milestone.Title.Equals(VersionMilestone))
        {
          continue;
        }

        if (Settings.IssueFilterLabel != null)
        {
          if (issue.Labels.Any(obj => obj.Name.ToUpper().Equals(Settings.IssueFilterLabel)))
          {
            continue;
          }
        }

        // Filter by issue label
        if (Settings.IssueLabels != null &&
            Settings.IssueLabels.Any())
        {
          foreach (var label in issue.Labels)
          {
            if (Settings.IssueLabels.Any(obj => obj.Key.Equals(label.Name.ToLower())))
            {
              issuesWithLabel.Add(new IssueWithLabel(label.Name, issue));
              break;
            }
          }
        }
        else
        {
          issuesWithLabel.Add(new IssueWithLabel(null, issue));
        }
      }

      // Build changelog text
      var issueGroups = issuesWithLabel.GroupBy(obj => obj.Label).OrderBy(obj => obj.Key);
      var changelog = GetChangelogFromIssues(issueGroups);

      return changelog;
    }

    private string GetChangelogFromIssues(IEnumerable<IGrouping<string, IssueWithLabel>> issueGroups)
    {
      var sb = new StringBuilder();
      foreach (var issueGroup in issueGroups)
      {
        sb.AppendLine();
        sb.AppendLine($"#### {issueGroup.Key}:");
        foreach (IssueWithLabel issueWithLabel in issueGroup.OrderBy(obj => obj.Issue.Title))
        {
          sb.AppendLine($"- [{issueWithLabel.Issue.Title}]({issueWithLabel.Issue.HtmlUrl})");
        }
      }
      string changelog = sb.ToString().Trim();
      return changelog;
    }

    public async Task CreateChangelogComplete()
    {
      Log.Information("Create Changelog...");

      StringBuilder sb = new StringBuilder();
      sb.AppendLine("Changelog"); // needed for broken encoding
      sb.AppendLine();

      var releases = await _client.Repository.Release.GetAll(ACCOUNT, REPO);
      foreach (var release in releases.OrderByDescending(obj => obj.CreatedAt.Date))
      {
        if (release.Draft)
        {
          continue;
        }

        var version = new Version(release.Name);
        var versionToDisplay = $"{version.Major}.{version.Minor}.{version.Build}";
        var dateTime = release.CreatedAt.DateTime.ToLocalTime();
        dateTime = dateTime.AddDays(1); // Don't know why but this is needed
        dateTime = dateTime.AddHours(1); // Think this is problem with summer & winter time
        var dateTimeToDisplay = dateTime.ToString("yyyy-MM-dd HH:mm");

        sb.AppendLine($"## [{versionToDisplay}]({release.HtmlUrl})");
        sb.AppendLine();

        if (release.Prerelease)
        {
          sb.AppendLine($"#### Build: {version.Revision} | Date: {dateTimeToDisplay} | Prerelease");
        }
        else
        {
          sb.AppendLine($"#### Build: {version.Revision} | Date: {dateTimeToDisplay}");
        }

        sb.AppendLine();

        string changeLog = release.Body.Trim();
        var changeLogLines = changeLog.Split('\n');
        foreach (var line in changeLogLines)
        {
          if (!line.StartsWith("|")) // Ignore Tables
          {
            sb.AppendLine(line);
          }
        }
        sb.AppendLine();
      }

      // Commit Changelog
      var changelog = sb.ToString();
      string path = "CHANGELOG.md";
      IReadOnlyList<RepositoryContent> contents = await _client
                                                        .Repository
                                                        .Content
                                                        .GetAllContents(ACCOUNT, REPO, path);

      await _client.Repository.Content.CreateFile(
        ACCOUNT,
        REPO,
        path,
        new UpdateFileRequest("Changelog",
                              changelog, contents.First().Sha));
    }
  }
}