using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Updater.Models;

namespace Updater.Services;

public interface IUpdaterService
{
    Task<ApplicationVersion> GetLatestVersionAsync(Guid applicationId);
    Task<CheckUpdateResult> CheckUpdateAsync(Guid applicationId);
    Task<List<FileMetadata>> GetFilesByVersionIdAsync(Guid versionId);

    Task ApplyUpdate(List<FileMetadata> remoteFiles, List<FileMetadata> localFiles,
        IProgress<ProgressReport> progress);
}