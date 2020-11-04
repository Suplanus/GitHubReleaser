using System;
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
      CommandLineParameters = new CommandLineParameters(args);
      if (CommandLineParameters.Result.HasErrors)
      {
        Log.Error($"Error in parameters: {CommandLineParameters.Result.ErrorText}");
        Environment.Exit(160);
      }
      foreach (var keyValuePair in CommandLineParameters.Result.AdditionalOptionsFound)
      {
        if (!string.IsNullOrWhiteSpace(keyValuePair.Value))
        {
          Log.Information($"{keyValuePair.Key}: {keyValuePair.Value}");  
        }
      }
      
      // Execute
      Releaser releaser = new Releaser(CommandLineParameters);
      releaser.Execute();
    }

    private static void LogParameter(string name, object value)
    {
      
    }
  }
}
