using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Aliyun.OSS;
using Aliyun.OSS.Common;
using Aliyun.OSS.Common.Authentication;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;
using Updater.Apis;
using Updater.Consts;
using Updater.Exceptions;
using Updater.Extensions;
using Updater.Helpers;
using Updater.Models;
using Updater.Services;
using Updater.ViewModels;
using Updater.Views;

namespace Updater;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static ServiceProvider Services { get; private set; } = null!;

    public static string RootPath => Directory.GetCurrentDirectory();

    public static string TempPath => System.IO.Path.Combine(RootPath, "UpdaterTemp");


    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        try
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);

            Log.Information("更新程序启动...");

            Services = await ConfigureServices();

            Log.Information("检查更新...");
            var config = Services.GetRequiredService<Config>();
            var updaterService = Services.GetRequiredService<IUpdaterService>();
            // 自动更新程序启动时检查更新
            // 从manifest.json中获取applicationId
            var result =
                await updaterService.CheckUpdateAsync(config.ApplicationId);
            var args = desktop.Args;
            if (args is { Length: > 0 })
            {
                switch (args[0])
                {
                    // 若第一个参数是 "--check-update" 或"-c"，则不打开GUI检查更新，通过返回值来判断是否有新版本
                    case "--check-update":
                    case "-c":
                    {
                        if (result.HasNewVersion)
                        {
                            // 若有新版本，则返回1
                            desktop.Shutdown(1);
                            return;
                        }

                        // 若没有新版本，则返回-1
                        desktop.Shutdown(-1);
                        return;
                    }
                    case "--update":
                    case "-u":
                    {
                        // 若第一个参数是 "--update" 或"-u"，则不打开GUI检查更新，直接更新
                        if (result.HasNewVersion)
                        {
                            // 若有新版本，则直接更新
                            Log.Information("发现新版本:{versionNumber}，进行更新...", result.LatestVersion?.VersionNumber);
                            new MainWindow().Show();
                        }
                        else
                        {
                            // 若没有新版本，则直接退出程序
                            await MessageBoxHelper.ShowInfo("当前已是最新版本");
                            Log.Information("当前已是最新版本，程序退出...");
                            desktop.Shutdown();
                        }

                        return;
                    }
                }
            }

            if (result.HasNewVersion)
            {
                // 若有新版本，则询问用户是否更新
                Log.Information("发现新版本:{versionNumber}", result.LatestVersion?.VersionNumber);
                var okOrNot = await MessageBoxManager
                    .GetMessageBoxStandard("检查更新", "发现新版本，是否更新？", ButtonEnum.YesNo, Icon.Info).ShowAsync();
                if (okOrNot == ButtonResult.Yes)
                {
                    Log.Information("进行更新...");
                    new MainWindow().Show();
                }
                else
                {
                    // 若用户选择不更新，则直接退出程序
                    Log.Information("用户选择不更新，退出程序");
                    desktop.Shutdown();
                }
            }
            else
            {
                // 如果没有新版本，则直接退出程序
                await MessageBoxHelper.ShowInfo("当前已是最新版本");
                Log.Information("当前已是最新版本，程序退出...");
                desktop.Shutdown();
            }
        }
        catch (TaskCanceledException e)
        {
            Log.Error(e, "TaskCanceledException");
            await MessageBoxHelper.ShowError("请求超时，请检查网络连接。");
            desktop.Shutdown();
        }
        catch (ConfigNotFoundException e)
        {
            Log.Error(e, "Config file not found.");
            await MessageBoxHelper.ShowError("未找到配置文件，请检查配置文件是否存在。");
            desktop.Shutdown(-999);
        }
        catch (NotInstallCurrectlyException e)
        {
            Log.Error(e, "Not installed correctly.");
            await MessageBoxHelper.ShowError("程序未正确安装，请重新安装本程序。");
            desktop.Shutdown(-999);
        }
        catch (HttpRequestException e)
        {
            Log.Warning(e, "Error occurred while sending request to server.");
            await MessageBoxHelper.ShowError("向服务器发送请求时发生错误，请检查网络连接。");
            desktop.Shutdown(-999);
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while executing the application.");
            await MessageBoxHelper.ShowError("程序运行过程中发生未知错误，请重新安装程序或联系开发者。");
            desktop.Shutdown(-999);
        }
    }

    private static async Task<ServiceProvider> ConfigureServices()
    {
        var collection = new ServiceCollection();
        collection.AddSingleton<MainViewModel>();
        collection.AddTransient<IUpdaterService, UpdaterService>();

        var config = await FileHelper.ReadConfigAsync();
        if (config is null)
        {
            throw new ConfigNotFoundException();
        }

        collection.AddSingleton(config);

        var manifest = await FileHelper.ReadManifestAsync();
        if (manifest is not null)
        {
            collection.AddSingleton(manifest);
        }

        collection.AddHttpApi<IApplicationVersionApi>().ConfigureHttpApi(options =>
        {
            options.PrependJsonSerializerContext(AppJsonSerializerContext.Default);
            options.HttpHost = new Uri($"{config.ServerUrl.TrimEnd('/')}/api/app/");
        });

        collection.AddHttpApi<IStsApi>().ConfigureHttpApi(options =>
        {
            options.PrependJsonSerializerContext(AppJsonSerializerContext.Default);
            options.HttpHost = new Uri($"{config.ServerUrl.TrimEnd('/')}/api/app/");
        });

        // 添加STS token 缓存
        var cache = new MemoryCache(Options.Create(new MemoryCacheOptions()));

        collection.AddSingleton<IMemoryCache>(cache);

        return collection.BuildServiceProvider();
    }
}

[JsonSerializable(typeof(ApplicationVersion))]
[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(FileMetadata))]
[JsonSerializable(typeof(Manifest))]
[JsonSerializable(typeof(Sts))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}