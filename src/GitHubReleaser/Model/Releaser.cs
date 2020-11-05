using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Octokit;

namespace GitHubReleaser.Model
{
  internal class Releaser
  {
    public ReleaserSettings Settings { get; set; }
    public GitHubClient Client { get; }
    public string Account { get; }
    public string Repo { get; }
    public string VersionMilestone { get; }
    public string VersionFull { get; }

    public Releaser(ReleaserSettings releaserSettings)
    {
      Settings = releaserSettings;
      var fileVersion = FileVersionInfo.GetVersionInfo(Settings.FileForVersion);
      var version = new Version(fileVersion.FileVersion);
      VersionMilestone = version.ToString(3);
      VersionFull = version.ToString();

      var split = Settings.GitHubRepo.Split('/');
      Account = split.First();
      Repo = split.Last();

      // GitHub
      ServicePointManager.SecurityProtocol =
        SecurityProtocolType.Tls12; // needed https://github.com/octokit/octokit.net/issues/1756
      var connection = new Connection(new ProductHeaderValue(Repo));
      Client = new GitHubClient(connection);
      var tokenAuth = new Credentials(Settings.GitHubToken);
      Client.Credentials = tokenAuth;
    }

    public async Task ExecuteAsync()
    {
      // Release
      ReleaseManager releaseManager = new ReleaseManager(this);
      Release release;
      if (Settings.IsUpdateOnly)
      {
        release = await releaseManager.UpdateRelease();
      }
      else
      {
        release = await releaseManager.CreateRelease();
      }

      // Changelog
      ChangelogManager changelogManager = new ChangelogManager(this);
      await changelogManager.Set(release);
      if (Settings.IsChangelogFileCreationEnabled)
      {
        await changelogManager.Set();
      }
    }
  }
}