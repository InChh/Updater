using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Microsoft.Extensions.Caching.Memory;
using Updater.Apis;
using Updater.Consts;
using Updater.Exceptions;
using Updater.Helpers;
using Updater.Models;

namespace Updater.Services;

public class UpdaterService(IApplicationVersionApi applicationVersionApi, Config config, IMemoryCache cache)
    : IUpdaterService
{
    public async Task<ApplicationVersion> GetLatestVersionAsync(Guid applicationId)
    {
        return await applicationVersionApi.GetLatestVersionAsync(applicationId);
    }

    public async Task<CheckUpdateResult> CheckUpdateAsync(Guid applicationId)
    {
        var latestVersion = await GetLatestVersionAsync(applicationId);
        var currentVersion = VersionHelper.GetCurrentVersion(applicationId);

        return new ApplicationVersionComparer().Compare(currentVersion.VersionNumber, latestVersion.VersionNumber) < 0
            ? new CheckUpdateResult(true, currentVersion, latestVersion)
            : new CheckUpdateResult(false, currentVersion);
    }


    public Task<List<FileMetadata>> GetFilesByVersionIdAsync(Guid versionId)
    {
        return applicationVersionApi.GetFilesByVersionIdAsync(versionId);
    }

    private static void ReportProgress(IProgress<ProgressReport> progress, ProgressReport data)
    {
        progress.Report(data);
    }

    private static DiffResult CalculateDiff(List<FileMetadata> localFiles, List<FileMetadata> remoteFiles,
        IProgress<ProgressReport> progress)
    {
        var diff = new DiffResult();
        var progressData = new ProgressReport()
        {
            Phase = "计算文件差异"
        };
        // 步骤1：建立查找字典（10%进度）
        progressData.TotalSteps = localFiles.Count + remoteFiles.Count + 2;
        progressData.CurrentStep = 0;
        var localPathDict = localFiles.ToDictionary(f => f.Path);
        var localHashDict = localFiles
            .GroupBy(f => f.Hash)
            .ToDictionary(g => g.Key, g => g.ToList());

        var remotePathDict = remoteFiles.ToDictionary(f => f.Path);
        progressData.CurrentStep = 1;
        progress.Report(progressData);

        // 步骤2：检测删除和重命名（40%进度）
        var deletePhaseSteps = localFiles.Count;
        progressData.TotalSteps += deletePhaseSteps;

        foreach (var localFile in localFiles)
        {
            progressData.CurrentFile = localFile.Path;
            progressData.CurrentStep++;

            if (!remotePathDict.ContainsKey(localFile.Path))
            {
                var renamedTarget = remoteFiles.FirstOrDefault(rf =>
                    rf.Hash == localFile.Hash &&
                    rf.Size == localFile.Size &&
                    rf.Path != localFile.Path);

                if (renamedTarget != null)
                {
                    diff.FilesToRename[localFile.Path] = renamedTarget.Path;
                }
                else
                {
                    diff.FilesToDelete.Add(localFile.Path);
                }
            }

            progress.Report(progressData);
        }

        // 步骤3：检测新增/更新（50%进度）
        progressData.Phase = "检测文件变更";
        foreach (var remoteFile in remoteFiles)
        {
            progressData.CurrentFile = remoteFile.Path;
            progressData.CurrentStep++;

            if (localPathDict.TryGetValue(remoteFile.Path, out var localFile))
            {
                if (localFile.Hash != remoteFile.Hash)
                {
                    diff.FilesToUpdate.Add(remoteFile);
                }
            }
            else if (!diff.FilesToRename.ContainsValue(remoteFile.Path))
            {
                diff.FilesToAdd.Add(remoteFile);
            }

            progress.Report(progressData);
        }

        return diff;
    }

    private async Task ApplyUpdateInner(DiffResult diff, IProgress<ProgressReport> progress)
    {
        var appRoot = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(appRoot, "UpdaterTemp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }
        // 清理临时文件夹中的文件

        foreach (var file in Directory.GetFiles(tempDir))
        {
            File.Delete(file);
        }

        var progressData = new ProgressReport { Phase = "应用更新" };
        var totalOperations = diff.FilesToRename.Count +
                              diff.FilesToDelete.Count +
                              diff.FilesToAdd.Count +
                              diff.FilesToUpdate.Count;

        progressData.TotalSteps = totalOperations;
        var completedSteps = 0;

        // // 阶段1：处理重命名
        // progressData.Phase = "处理文件重命名";
        // foreach (var rename in diff.FilesToRename)
        // {
        //     progressData.CurrentFile = $"{rename.Key} → {rename.Value}";
        //     progressData.CurrentStep = ++completedSteps;
        //
        //     var oldPath = Path.Combine(appRoot, rename.Key);
        //     var newPath = Path.Combine(appRoot, rename.Value);
        //
        //     if (File.Exists(oldPath))
        //     {
        //         Directory.CreateDirectory(Path.GetDirectoryName(newPath) ?? throw new InvalidOperationException());
        //         File.Move(oldPath, newPath);
        //     }
        //
        //     ReportProgress(progress, progressData);
        // }

        // // 阶段2：处理删除
        // progressData.Phase = "清理旧文件";
        // foreach (var path in diff.FilesToDelete)
        // {
        //     progressData.CurrentFile = path;
        //     progressData.CurrentStep = ++completedSteps;
        //
        //     var fullPath = Path.Combine(appRoot, path);
        //     if (File.Exists(fullPath))
        //     {
        //         // 安全考虑，不直接删除文件
        //     }
        //
        //     ReportProgress(progress, progressData);
        // }

        // 阶段3：处理下载
        progressData.Phase = "下载更新文件";
        var allUpdates = diff.FilesToAdd.Concat(diff.FilesToUpdate).ToList();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 8 };

        await Parallel.ForEachAsync(allUpdates, options, async (file, ctk) =>
        {
            var localProgress = new ProgressReport
            {
                CurrentFile = file.Path,
                CurrentStep = Interlocked.Increment(ref completedSteps)
            };

            var fileName = file.Path.Split('/').Last();
            var isSelfUpdate = fileName is FileNameConsts.UpdaterFileName or FileNameConsts.MainExeFileName;

            try
            {
                var tempPath = Path.Combine(tempDir, file.Hash);
                var targetPath = Path.Combine(appRoot, file.Path);

                if (!File.Exists(tempPath))
                {
                    var ossClient = await cache.GetOssClient(config);

                    var response = ossClient.GetObject(config.BucketName, $"{file.Hash}/{fileName}");

                    // 实现下载进度报告
                    var totalBytes = response.ContentLength;
                    var buffer = new byte[8192];
                    long totalRead = 0;

                    await using var stream = response.Content;
                    await using var fs = new FileStream(tempPath, FileMode.Create);

                    int read;
                    while ((read = await stream.ReadAsync(buffer, ctk)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), ctk);
                        totalRead += read;

                        // 报告下载字节进度
                        var downloadProgress = new ProgressReport()
                        {
                            Phase = "下载文件",
                            CurrentFile = file.Path,
                            ProcessedBytes = totalRead,
                            TotalBytes = totalBytes,
                            CurrentStep = completedSteps
                        };
                        progress?.Report(downloadProgress);
                    }
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? throw new InvalidOperationException());
                if (!isSelfUpdate)
                {
                    File.Move(tempPath, targetPath, true);
                }
                else
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = FileNameConsts.SelfUpdateFileName,
                        ArgumentList = { tempPath, targetPath, Environment.ProcessId.ToString() },
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    };

                    Process.Start(info);
                }
            }
            finally
            {
                progress?.Report(localProgress);
            }
        });
    }

    public async Task ApplyUpdate(List<FileMetadata> remoteFiles, List<FileMetadata> localFiles,
        IProgress<ProgressReport> progress)
    {
        var backup = new Dictionary<string, string>();
        var diff = await Task.Run(() => CalculateDiff(localFiles, remoteFiles, progress));

        var appRoot = Directory.GetCurrentDirectory();
        try
        {
            // 备份可能被修改的文件
            foreach (var file in diff.FilesToUpdate)
            {
                var path = Path.Combine(appRoot, file.Path);
                if (!File.Exists(path)) continue;
                var temp = Path.GetTempFileName();
                File.Copy(path, temp, true);
                backup[file.Path] = temp;
            }

            // 执行更新
            await ApplyUpdateInner(diff, progress);
        }
        catch (IOException e)
        {
            // 回滚修改
            foreach (var entry in backup)
            {
                File.Copy(entry.Value, Path.Combine(appRoot, entry.Key), true);
            }

            throw new UpdateException("更新失败，请尝试重新运行更新程序", e);
        }
        catch (Exception)
        {
            // 回滚修改
            foreach (var entry in backup)
            {
                File.Copy(entry.Value, Path.Combine(appRoot, entry.Key), true);
            }

            throw;
        }
        finally
        {
            // 清理临时文件
            foreach (var tempFile in backup.Values)
            {
                File.Delete(tempFile);
            }
        }
    }
}