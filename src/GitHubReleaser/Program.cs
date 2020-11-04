using System;
using System.Collections.Generic;
using System.Linq;
using GitHubReleaser.Model;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace GitHubReleaser
{
  class Program
  {
    public static CommandLineParameters CommandLineParameters { get; private set; }

    static void Main(string[] args)
    {
      // Setup
      var argumentString = string.Join(" ", args.Skip(1));
      Console.Title = $@"{nameof(GitHubReleaser)} {argumentString}";

      Log.Logger = new LoggerConfiguration()
                   .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                   .CreateLogger();

      // CommandLineParameters
      CommandLineParameters = CommandLineParameters.GetCommandLineParameters(args);
      if (CommandLineParameters.Result.HasErrors)
      {
        Log.Error($"Error in parameters: {CommandLineParameters.Result.ErrorText}");
        Environment.Exit(160);
      }
      LogParameter(nameof(CommandLineParameters.GitHubRepo), CommandLineParameters.GitHubRepo);
      LogParameter(nameof(CommandLineParameters.GitHubToken), CommandLineParameters.GitHubToken);
      LogParameter(nameof(CommandLineParameters.FileForVersion), CommandLineParameters.FileForVersion);
      LogParameter(nameof(CommandLineParameters.IsChangelogFileCreationEnabled), CommandLineParameters.IsChangelogFileCreationEnabled);
      LogParameter(nameof(CommandLineParameters.IsPreRelease), CommandLineParameters.IsPreRelease);
      LogParameter(nameof(CommandLineParameters.IsUpdateOnly), CommandLineParameters.IsUpdateOnly);
      LogParameter(nameof(CommandLineParameters.IssueFilterLabel), CommandLineParameters.IssueFilterLabel);
      LogParameter(nameof(CommandLineParameters.IssueLabels), CommandLineParameters.IssueLabels);
      LogParameter(nameof(CommandLineParameters.ReleaseAttachments), CommandLineParameters.ReleaseAttachments);

      // Execute
      Releaser releaser = new Releaser(CommandLineParameters);
      releaser.Execute();
    }

    private static void LogParameter(string name, object value)
    {
      if (!string.IsNullOrWhiteSpace(value?.ToString()))
      {
        if (value is List<string> list)
        {
          string listValue = string.Empty;
          foreach (var item in list)
          {
            listValue = listValue + Environment.NewLine + "\t\t" + item;
          }
          Log.Information($"{name}: {listValue}");  
        }
        else
        {
          Log.Information($"{name}: {value}");  
        }
      }
    }
  }
}
