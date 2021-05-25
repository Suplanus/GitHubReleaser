using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Fclp;
using Serilog;

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

      parser.Setup(arg => arg.DeleteFilesAfterUpload)
            .As("delete-files-after-upload");

      var result = parser.Parse(args);

      var commandLineArguments = parser.Object;

      // Manual map
      if (parser.Object.IssueLabelsWithHeader != null)
      {
        parser.Object.IssueLabels = new Dictionary<string, string>();
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

    public static CommandLineParameters FromFile(string[] args)
    {
      if (args.Length != 1)
      {
        return null;
      }

      var configFile = args[0];

      if (!File.Exists(configFile))
      {
        Log.Error($"File does not exits: {configFile}");
        throw new FileNotFoundException("The file name passed with the argument 'config-file' does not exist",
                                        configFile);
      }

      var commandLineParameters = new CommandLineParameters();
      ReleaserSettings settings;
      var extension = Path.GetExtension(configFile);
      switch (extension)
      {
        case ".json":
          settings = JsonDeserialize(configFile);
          break;
        case ".xml":
          settings = XmlDeserialize(configFile);
          break;
        default:
          Log.Error($"Unknown file type {Path.GetExtension(configFile)}");
          throw new ArgumentException("The file name passed with the argument has an unknown file extension.",
                                      "config-file");
      }
      commandLineParameters.MapProperties(settings);
      
      var parser = new FluentCommandLineParser<CommandLineParameters>();
      parser.Setup(arg => arg.ConfigFile)
            .As("config-file"); //dummy
      commandLineParameters.Result = parser.Parse(args);
      commandLineParameters.ConfigFile = configFile;
      return commandLineParameters;
    }

    private static ReleaserSettings JsonDeserialize(string configFile)
    {
      try
      {
        var jsonString = File.ReadAllText(configFile);
        var settings = JsonSerializer.Deserialize<ReleaserSettings>(jsonString);
        return settings;
      }
      catch (Exception e)
      {
        Log.Error(e.Message);
        throw;
      }
    }

    private void MapProperties(ReleaserSettings settings)
    {
      IsChangelogFileCreationEnabled = settings.IsChangelogFileCreationEnabled;
      IsUpdateOnly = settings.IsUpdateOnly;
      ReleaseAttachments = settings.ReleaseAttachments;
      IssueFilterLabel = settings.IssueFilterLabel;
      IssueLabels = settings.IssueLabels;
      IsPreRelease = settings.IsPreRelease;
      IsDraft = settings.IsDraft;
      DeleteFilesAfterUpload = settings.DeleteFilesAfterUpload;
      FileForVersion = settings.FileForVersion;
      GitHubToken = settings.GitHubToken;
      GitHubRepo = settings.GitHubRepo;
    }

    private static ReleaserSettings XmlDeserialize(string configFile)
    {
      throw new NotImplementedException();
    }
  }
}
