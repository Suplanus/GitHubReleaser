using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Serilog;

namespace GitHubReleaser.Model
{
  internal class ReleaseManager
  {
    private readonly Releaser _releaser;

    public ReleaseManager(Releaser releaser)
    {
      _releaser = releaser;
    }

    public async Task<Release> UpdateRelease()
    {
      IReadOnlyList<Release> releases = await _releaser.Client.Repository.Release.GetAll(_releaser.Account, _releaser.Repo); // Get() throw exception if not found
      Release release = releases.FirstOrDefault(obj => obj.Name.Equals(_releaser.VersionFull));
      if (release == null)
      {
        Log.Error("Release to update not found");
        Environment.Exit(160);
      }

      ReleaseUpdate updateRelease = release.ToUpdate();
      updateRelease.Draft = _releaser.Settings.IsDraft;
      var result = await _releaser.Client.Repository.Release.Edit(_releaser.Account, _releaser.Repo, release.Id, updateRelease);

      // Attachments
      if (result.Assets.Any())
      {
        foreach (var asset in result.Assets)
        {
          await _releaser.Client.Repository.Release.DeleteAsset(_releaser.Account, _releaser.Repo, asset.Id);
        }
      }
      await UploadAttachments(result);
      return result;
    }

    public async Task<Release> CreateRelease()
    {
      // Remove existing
      IReadOnlyList<Release> releases = await _releaser.Client.Repository.Release.GetAll(_releaser.Account, _releaser.Repo); // Get() throw exception if not found
      var release = releases.FirstOrDefault(obj => obj.Name.Equals(_releaser.VersionFull));
      if (release != null)
      {
        Log.Information("Remove release...");
        await _releaser.Client.Repository.Release.Delete(_releaser.Account, _releaser.Repo, release.Id);

        // await _client.Git.Reference.Delete(ACCOUNT, REPO, VersionFull); // todo: Delete tag
      }

      // Create
      Log.Information("Create release...");
      NewRelease newRelease = new NewRelease(_releaser.VersionFull);
      newRelease.Name = _releaser.VersionFull;
      newRelease.Prerelease = _releaser.Settings.IsPreRelease;
      newRelease.Draft = _releaser.Settings.IsDraft;

      release = await _releaser.Client.Repository.Release.Create(_releaser.Account, _releaser.Repo, newRelease);

      // Upload: Attachments
      await UploadAttachments(release);
      return release;
    }

    private async Task UploadAttachments(Release release)
    {
      if (_releaser.Settings.ReleaseAttachments == null ||
          !_releaser.Settings.ReleaseAttachments.Any())
      {
        return;
      }

      Log.Information("Upload attachments...");
      bool deleteFilesAfterUpload = _releaser.Settings.DeleteFilesAfterUpload;
      try
      {
        for (var index = 0; index < _releaser.Settings.ReleaseAttachments.Count; index++)
        {
          Log.Information(index + 1 + " / " + _releaser.Settings.ReleaseAttachments.Count);

          var setupFile = _releaser.Settings.ReleaseAttachments[index];
          using var archiveContents = File.OpenRead(setupFile);
          string assetFilename = Path.GetFileName(setupFile);
          var assetUpload = new ReleaseAssetUpload
          {
            FileName = assetFilename,
            ContentType = "application/x-msdownload",
            RawData = archiveContents
          };
          await _releaser.Client.Repository.Release.UploadAsset(release, assetUpload);
        }
      }
      catch (Exception exception)
      {
        Log.Error(exception.ToString());
        deleteFilesAfterUpload = false;
      }

      if (deleteFilesAfterUpload)
      {
        foreach (var attachment in _releaser.Settings.ReleaseAttachments)
        {
          File.Delete(attachment);
        }
      }
    }
  }
}