using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitHubReleaser.Model;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace GitHubReleaser
{
  class Program
  {
    static async Task<int> Main(string[] args)
    {
      // Setup
      var argumentString = string.Join(" ", args.Skip(1));
      Console.Title = $@"{nameof(GitHubReleaser)} {argumentString}";

      Log.Logger = new LoggerConfiguration()
                   .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                   .CreateLogger();

      // CommandLineParameters
      var commandLine = GetCommandline(args);

      // Execute
      Releaser releaser = new Releaser(commandLine);
      await releaser.ExecuteAsync();

      Log.Information("All looks good, have fun with your release!");

      return 0;
    }

    private static CommandLineParameters GetCommandline(string[] args)
    {
      var commandLineParameters = CommandLineParameters.FromArguments(args);

      // Checks
      if (commandLineParameters.Result.HasErrors)
      {
        Log.Error($"Error in parameters: {commandLineParameters.Result.ErrorText}");
        Environment.Exit(160);
      }

      commandLineParameters.FileForVersion = Path.GetFullPath(commandLineParameters.FileForVersion);
      if (!File.Exists(commandLineParameters.FileForVersion))
      {
        Log.Error($"File not exists: {commandLineParameters.FileForVersion}");
        Environment.Exit(160);
      }

      var extension = Path.GetExtension(commandLineParameters.FileForVersion)?.ToLower();
      if (extension != ".dll" &&
          extension != ".exe")
      {
        Log.Error($"File type not supported: {extension}");
        Environment.Exit(160);
      }

      foreach (var attachment in commandLineParameters.ReleaseAttachments)
      {
        if (!File.Exists(attachment))
        {
          Log.Error($"Attachment file not found: {attachment}");
          Environment.Exit(160);
        }
      }

      LogParameter(nameof(commandLineParameters.GitHubRepo), commandLineParameters.GitHubRepo);
      LogParameter(nameof(commandLineParameters.GitHubToken), commandLineParameters.GitHubToken);
      LogParameter(nameof(commandLineParameters.FileForVersion), commandLineParameters.FileForVersion);
      LogParameter(nameof(commandLineParameters.IsChangelogFileCreationEnabled),
                   commandLineParameters.IsChangelogFileCreationEnabled);
      LogParameter(nameof(commandLineParameters.IsPreRelease), commandLineParameters.IsPreRelease);
      LogParameter(nameof(commandLineParameters.IsUpdateOnly), commandLineParameters.IsUpdateOnly);
      LogParameter(nameof(commandLineParameters.IssueFilterLabel), commandLineParameters.IssueFilterLabel);
      LogParameter(nameof(commandLineParameters.IssueLabels), commandLineParameters.IssueLabels);
      LogParameter(nameof(commandLineParameters.ReleaseAttachments), commandLineParameters.ReleaseAttachments);

      return commandLineParameters;
    }

    private static void LogParameter(string name, object value)
    {
      if (!string.IsNullOrWhiteSpace(value?.ToString()))
      {
        switch (value)
        {
          case List<string> list:
          {
            string listValue = string.Empty;
            foreach (var item in list)
            {
              listValue = listValue + Environment.NewLine + "\t\t" + item;
            }
            Log.Information($"{name}: {listValue}");
            break;
          }
          case Dictionary<string,string> list:
          {
            string listValue = string.Empty;
            foreach (var item in list)
            {
              listValue = listValue + Environment.NewLine + "\t\t" + item.Key +" -> " + item.Value;
            }
            Log.Information($"{name}: {listValue}");
            break;
          }
          default:
            Log.Information($"{name}: {value}");
            break;
        }
      }
    }
  }
}
