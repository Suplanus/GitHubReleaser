using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using System.Text.Json.Serialization.Converters;
using Serilog;

namespace GitHubReleaser.Model
{
  internal class ReleaserSettings
  {
    public string ConfigFile { get; set; }

    public bool IsChangelogFileCreationEnabled { get; set; }

    public bool IsUpdateOnly { get; set; }

    public List<string> ReleaseAttachments { get; set; }

    public string IssueFilterLabel { get; set; }

    public Dictionary<string, string> IssueLabels { get; set; }

    public bool IsPreRelease { get; set; }

    public bool IsDraft { get; set; }

    public bool DeleteFilesAfterUpload { get; set; }

    public string FileForVersion { get; set; }

    public string GitHubToken { get; set; }

    public string GitHubRepo { get; set; }
  }
}