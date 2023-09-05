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
      try
      {
        Releaser releaser = new Releaser(commandLine);
        await releaser.ExecuteAsync();
        Log.Information("All looks good, have fun with your release!");
        return 0;
      }
      catch
      {
        return 100;
      }
    }

    private static CommandLineParameters GetCommandline(string[] args)
    {
      CommandLineParameters commandLineParameters = CommandLineParameters.FromFile(args);

      if (commandLineParameters == null)
      {
        commandLineParameters = CommandLineParameters.FromArguments(args);
      }

      // Checks
      if (commandLineParameters.Result.HasErrors)
      {
        ErrorHandler.Log($"Error in parameters: {commandLineParameters.Result.ErrorText}");
      }

      commandLineParameters.FileForVersion = Path.GetFullPath(commandLineParameters.FileForVersion);
      if (!File.Exists(commandLineParameters.FileForVersion))
      {
        ErrorHandler.Log($"File not exists: {commandLineParameters.FileForVersion}");
      }

      var extension = Path.GetExtension(commandLineParameters.FileForVersion)?.ToLower();
      if (extension != ".dll" &&
          extension != ".exe")
      {
        ErrorHandler.Log($"File type not supported: {extension}");
      }

      if (commandLineParameters.ReleaseAttachments != null)
      {
        for (var index = 0; index < commandLineParameters.ReleaseAttachments.Count; index++)
        {
          commandLineParameters.ReleaseAttachments[index] =
            Path.GetFullPath(commandLineParameters.ReleaseAttachments[index]);
          var attachment = commandLineParameters.ReleaseAttachments[index];
          if (!File.Exists(attachment))
          {
            ErrorHandler.Log($"Attachment file not found: {attachment}");
          }
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
          case Dictionary<string, string> list:
          {
            string listValue = string.Empty;
            foreach (var item in list)
            {
              listValue = listValue + Environment.NewLine + "\t\t" + item.Key + " -> " + item.Value;
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