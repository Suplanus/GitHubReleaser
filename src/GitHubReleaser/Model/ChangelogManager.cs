using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace GitHubReleaser.Model
{
  internal class ChangelogManager
  {
    private readonly Releaser _releaser;

    private static readonly string Alert = "| :warning: **Pre-release**|" +
                                           Environment.NewLine +
                                           "| --- |";

    public ChangelogManager(Releaser releaser)
    {
      _releaser = releaser;
    }

    public async Task Set(Release release)
    {
      ReleaseUpdate updateRelease = release.ToUpdate();
      updateRelease.Body = await GetReleaseChangelog();
      await _releaser.Client.Repository.Release.Edit(_releaser.Account, _releaser.Repo, release.Id, updateRelease);
    }

    private async Task<string> GetReleaseChangelog()
    {
      DateTimeOffset? lastReleaseCreatedDate = null;
      if (_releaser.Settings.IsPreRelease)
      {
        var releases = await _releaser.Client.Repository.Release.GetAll(_releaser.Account, _releaser.Repo);
        Release lastRelease = releases.OrderBy(obj => obj.CreatedAt.DateTime).LastOrDefault();
        if (lastRelease != null)
        {
          lastReleaseCreatedDate = lastRelease.PublishedAt;
        }
      }

      var repository = await _releaser.Client.Repository.Get(_releaser.Account, _releaser.Repo);
      var allIssues = await _releaser.Client.Issue.GetAllForRepository(repository.Id,
                                                                       new RepositoryIssueRequest
                                                                         { State = ItemStateFilter.Closed });
      var issuesWithLabel = new List<IssueWithLabel>();
      foreach (var issue in allIssues)
      {
        if (issue.Milestone == null)
        {
          continue;
        }

        if (!issue.Milestone.Title.Equals(_releaser.VersionMilestone))
        {
          continue;
        }

        if (_releaser.Settings.IssueFilterLabel != null)
        {
          if (issue.Labels.Any(obj => obj.Name.ToLower().Equals(_releaser.Settings.IssueFilterLabel.ToLower())))
          {
            continue;
          }
        }

        if (lastReleaseCreatedDate != null)
        {
          if (issue.ClosedAt <= lastReleaseCreatedDate)
          {
            continue;
          }
        }

        // Filter by issue label
        if (_releaser.Settings.IssueLabels != null &&
            _releaser.Settings.IssueLabels.Any())
        {
          foreach (var label in issue.Labels)
          {
            _releaser.Settings.IssueLabels.TryGetValue(label.Name, out var labelHeader);
            if (labelHeader != null)
            {
              issuesWithLabel.Add(new IssueWithLabel(labelHeader, issue));
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

      if (_releaser.Settings.IsPreRelease)
      {
        changelog = Alert + Environment.NewLine + changelog;
      }

      return changelog;
    }

    private string GetChangelogFromIssues(IEnumerable<IGrouping<string, IssueWithLabel>> issueGroups)
    {
      Log.Information("Issues:");
      var sb = new StringBuilder();
      foreach (var issueGroup in issueGroups)
      {
        sb.AppendLine();
        sb.AppendLine($"#### {issueGroup.Key}:");
        Console.WriteLine("\t" + issueGroup.Key);
        foreach (IssueWithLabel issueWithLabel in issueGroup.OrderBy(obj => obj.Issue.Title))
        {
          sb.AppendLine($"- [{issueWithLabel.Issue.Title}]({issueWithLabel.Issue.HtmlUrl})");
          Console.WriteLine("\t\t" + issueWithLabel.Issue.Title);
        }
      }
      string changelog = sb.ToString().Trim();
      return changelog;
    }

    internal async Task Set()
    {
      Log.Information("Create Changelog...");

      StringBuilder sb = new StringBuilder();

      var releases = await _releaser.Client.Repository.Release.GetAll(_releaser.Account, _releaser.Repo);
      foreach (var release in releases.OrderByDescending(obj => obj.CreatedAt.Date))
      {
        if (release.Draft)
        {
          continue;
        }

        var version = new Version(release.Name);
        var versionToDisplay = $"{version.Major}.{version.Minor}.{version.Build}";
        var dateTime = release.CreatedAt.DateTime.ToUniversalTime();
        dateTime = dateTime.AddDays(1); // Don't know why this is needed
        var dateTimeToDisplay = dateTime.ToString("yyyy-MM-dd HH:mm");

        sb.AppendLine($"## [{versionToDisplay}]({release.HtmlUrl})");
        sb.AppendLine();

        if (release.Prerelease)
        {
          sb.AppendLine($"`Build: {version.Revision} | Date (UTC): {dateTimeToDisplay} | Pre-release`");
        }
        else
        {
          sb.AppendLine($"`Build: {version.Revision} | Date (UTC): {dateTimeToDisplay}`");
        }

        sb.AppendLine();

        string changeLog = release.Body.Trim();
        changeLog = changeLog.Replace(Alert, ""); // Remove alert
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
      IReadOnlyList<RepositoryContent> contents = await _releaser.Client
                                                                 .Repository
                                                                 .Content
                                                                 .GetAllContents(_releaser.Account, _releaser.Repo);
      string path = "CHANGELOG.md";
      RepositoryContent content = contents.FirstOrDefault(obj => obj.Path.Equals(path));
      if (content == null)
      {
        Log.Error("Changelog.md not found");
        Environment.Exit(160);
      }

      await _releaser.Client.Repository.Content.CreateFile(
        _releaser.Account,
        _releaser.Repo,
        path,
        new UpdateFileRequest("Changelog",
                              changelog, content.Sha));

      Console.WriteLine(changelog);
    }
  }
}