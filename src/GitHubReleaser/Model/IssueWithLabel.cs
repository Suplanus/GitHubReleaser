using Octokit;

namespace GitHubReleaser.Model
{
  internal class IssueWithLabel
  {
    public string Label { get; }
    public Issue Issue { get; }

    public IssueWithLabel(string label, Issue issue)
    {
      Label = label;
      Issue = issue;
    }
  }
}