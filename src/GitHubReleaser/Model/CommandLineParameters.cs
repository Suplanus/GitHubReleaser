using System.Runtime.CompilerServices;
using Fclp;

namespace GitHubReleaser.Model
{
  internal class CommandLineParameters : ReleaserSettings
  {
    public ICommandLineParserResult Result { get; set; }

    public static CommandLineParameters GetCommandLineParameters(string[] args)
    {
      var parser = new FluentCommandLineParser<CommandLineParameters>();
      parser.Setup(arg => arg.GitHubRepo)
            .As("github-repo")
            .Required();

      parser.Setup(arg => arg.GitHubToken)
            .As("github-token")
            .Required();

      parser.Setup(arg => arg.FileForVersion)
            .As("file-for-version")
            .Required();

      parser.Setup(arg => arg.IsPreRelease)
            .As("pre-release");

      parser.Setup(arg => arg.IssueLabels)
            .As("issue-labels");

      parser.Setup(arg => arg.IssueFilterLabel)
            .As("issue-filter-label");

      parser.Setup(arg => arg.ReleaseAttachments)
            .As("release-attachments");

      parser.Setup(arg => arg.IsUpdateOnly)
            .As("update-only");

      parser.Setup(arg => arg.IsChangelogFileCreationEnabled)
            .As("create-changelog-file");

      var result = parser.Parse(args);
      var commandLineArguments = parser.Object;
      commandLineArguments.Result = result;

#if DEBUG
      commandLineArguments.GitHubToken = Secrets.GitHubToken;
#endif
      return commandLineArguments;
    }
  }

  internal static class Secrets
  {
    public static string GitHubToken => "9065c29143e2e1ac196bd91b13418c8e9677ac97";
  }
}