using System.Collections.Generic;
using System.Linq;
using Fclp;

namespace GitHubReleaser.Model
{
  internal class CommandLineParameters : ReleaserSettings
  {
    public ICommandLineParserResult Result { get; set; }

    public List<string> IssueLabelsWithHeader { get; set; }

    public static CommandLineParameters FromArguments(string[] args)
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

      parser.Setup(arg => arg.IssueLabelsWithHeader)
            .As("issue-labels");

      parser.Setup(arg => arg.IssueFilterLabel)
            .As("issue-filter-label");

      parser.Setup(arg => arg.ReleaseAttachments)
            .As("release-attachments");

      parser.Setup(arg => arg.IsUpdateOnly)
            .As("update-only");

      parser.Setup(arg => arg.IsChangelogFileCreationEnabled)
            .As("create-changelog-file");

      parser.Setup(arg => arg.IsDraft)
            .As("draft");

      var result = parser.Parse(args);
      var commandLineArguments = parser.Object;

      // Manual map
      if (parser.Object.IssueLabelsWithHeader != null)
      {
        foreach (var issueLabelWithHeader in parser.Object.IssueLabelsWithHeader)
        {
          var split = issueLabelWithHeader.Split(';');
          parser.Object.IssueLabels.Add(split.First(), split.Last());
        }
      }

      commandLineArguments.Result = result;

#if DEBUG
      commandLineArguments.GitHubToken = Secrets.GitHubToken;
#endif
      return commandLineArguments;
    }
  }
}