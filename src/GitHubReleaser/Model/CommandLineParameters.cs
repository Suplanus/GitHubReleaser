using Fclp;

namespace GitHubReleaser.Model
{
  internal class CommandLineParameters : ReleaserSettings
  {
    public ICommandLineParserResult Result { get; set; }

    public FluentCommandLineParser<CommandLineParameters> Parser { get; set; }

    public CommandLineParameters(string[] args)
    {
      Parser = new FluentCommandLineParser<CommandLineParameters>();
      Setup();
      Result = Parser.Parse(args);
    }

    public CommandLineParameters() {}

    private void Setup()
    {
      Parser.Setup(arg => arg.GitHubRepo)
            .As("github-repo")
            .Required();

      Parser.Setup(arg => arg.GitHubToken)
            .As("github-token")
            .Required();

      Parser.Setup(arg => arg.FileForVersion)
            .As("file-for-version")
            .Required();

      Parser.Setup(arg => arg.IsPreRelease)
            .As("pre-release");

      Parser.Setup(arg => arg.IssueLabels)
            .As("issue-labels	");

      Parser.Setup(arg => arg.IssueFilterLabel)
            .As("issue-filter-label	");

      Parser.Setup(arg => arg.ReleaseAttachments)
            .As("release-attachments");

      Parser.Setup(arg => arg.IsUpdateOnly)
            .As("update-only");

      Parser.Setup(arg => arg.IsChangelogFileCreationEnabled)
            .As("create-changelog-file");
    }
  }
}