using System.Collections.Generic;

namespace GitHubReleaser.Model
{
  internal class ReleaserSettings
  {
    public bool IsChangelogFileCreationEnabled { get; set; }

    public bool IsUpdateOnly { get; set; }

    public List<string> ReleaseAttachments { get; set; }

    public string IssueFilterLabel { get; set; }

    public Dictionary<string, string> IssueLabels { get; set; }

    public bool IsPreRelease { get; set; }

    public bool IsDraft { get; set; }

    public string FileForVersion { get; set; }

    public string GitHubToken { get; set; }

    public string GitHubRepo { get; set; }
  }
}