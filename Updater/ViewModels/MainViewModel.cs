using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using Serilog;
using Updater.Exceptions;
using Updater.Extensions;
using Updater.Helpers;
using Updater.Models;
using Updater.Services;

namespace Updater.ViewModels;

public partial class MainViewModel(IUpdaterService updaterService, Config config) : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia!";
    [ObservableProperty] private string _currentProgressMessage = string.Empty;
    [ObservableProperty] private double _currentProgressPercentage = 100;

    [ObservableProperty] private string _globalProgressMessage = string.Empty;

    [ObservableProperty] private double _globalProgressPercentage;

    [ObservableProperty] private string _versionDescription = string.Empty;

    [RelayCommand]
    private async Task UpdateAsync()
    {
        try
        {
            var progress =
                new Progress<ProgressReport>(report =>
                {
                    var sb = new StringBuilder();
                    CurrentProgressPercentage = report.ProgressPercentage;
                    sb.Append($"{report.Phase} : {report.CurrentFile}");
                    if (report.ProcessedBytes > 0)
                    {
                        sb.Append($"\t(已传输: {FileHelper.FormatBytes(report.ProcessedBytes)})");
                    }

                    CurrentProgressMessage = sb.ToString();
                });

            GlobalProgressMessage = "正在检查更新...";
            var result = await updaterService.CheckUpdateAsync(config.ApplicationId);
            GlobalProgressPercentage = 30;
            if (!result.HasNewVersion)
            {
                GlobalProgressPercentage = 100;
                GlobalProgressMessage = "当前已是最新版本";
                await MessageBoxHelper.ShowInfo("当前已是最新版本");
                Environment.Exit(1);
                return;
            }

            GlobalProgressMessage = "正在获取文件列表...";
            var latestVersion = result.LatestVersion!;
            var files = await updaterService.GetFilesByVersionIdAsync(latestVersion.Id);
            GlobalProgressPercentage = 50;

            GlobalProgressMessage = "正在校验本地文件...";
            // 从manifest文件中获取本地文件列表
            List<FileMetadata> localFiles;
            if (App.Services.GetService(typeof(Manifest)) is Manifest manifest)
            {
                localFiles = manifest.Files;
            }
            else
            {
                // 若manifest文件不存在，则重新计算本地文件列表
                localFiles = await FileHelper.GetLocalFiles(Directory.GetCurrentDirectory());
            }

            await updaterService.ApplyUpdate(files, localFiles, progress);

            // 更新本地manifest文件
            var newManifest = new Manifest()
            {
                ApplicationId = config.ApplicationId,
                VersionId = latestVersion.Id,
                VersionNumber = latestVersion.VersionNumber,
                Description = latestVersion.Description,
                Files = files
            };
            await FileHelper.WriteManifestAsync(newManifest);
            // 更新注册表
            VersionHelper.SetCurrentVersion(config.ApplicationId, latestVersion.VersionNumber);

            GlobalProgressPercentage = 100;
            GlobalProgressMessage = "更新完成";

            await MessageBoxHelper.ShowInfo("更新完成");
            Environment.Exit(0);
        }
        catch (ConfigNotFoundException e)
        {
            Log.Error(e, "Config file not found.");
            await MessageBoxHelper.ShowError("未找到配置文件，请检查配置文件是否存在。");
            Environment.Exit(-999);
        }
        catch (OperationCanceledException e)
        {
            Log.Error(e, "");
            await MessageBoxHelper.ShowError(e.Message);
            Environment.Exit(-999);
        }
        catch (NotInstallCurrectlyException e)
        {
            Log.Error(e, "Not installed correctly.");
            await MessageBoxHelper.ShowError("程序未正确安装，请重新安装本程序。");
            Environment.Exit(-999);
        }
        catch (HttpRequestException e)
        {
            Log.Warning(e, "Error occurred while sending request to server.");
            await MessageBoxHelper.ShowError("向服务器发送请求时发生错误，请检查网络连接。");
            Environment.Exit(-999);
        }
        catch (UpdateException e)
        {
            Log.Error(e, e.Message);
            await MessageBoxHelper.ShowError(e.Message);
            Environment.Exit(-999);
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while executing the application.");
            await MessageBoxHelper.ShowError("程序运行过程中发生未知错误，请重新安装程序或联系开发者。");
            Environment.Exit(-999);
        }
    }
}