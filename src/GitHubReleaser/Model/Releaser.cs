namespace GitHubReleaser.Model
{
  internal class Releaser
  {
    public Releaser(ReleaserSettings releaserSettings)
    {
      Settings = releaserSettings;
    }

    public ReleaserSettings Settings { get; set; }

    public void Execute()
    {
      
    }
  }
}