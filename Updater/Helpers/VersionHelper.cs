using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Win32;
using Updater.Extensions;
using Updater.Models;

namespace Updater.Helpers;

[SuppressMessage("Interoperability", "CA1416:验证平台兼容性")]
public static class VersionHelper
{
    public static ApplicationVersion GetCurrentVersion(Guid applicationId)
    {
        if (App.Services.GetService(typeof(Manifest)) is Manifest manifest)
        {
            return new ApplicationVersion
            {
                Id = manifest.VersionId,
                VersionNumber = manifest.VersionNumber,
                Description = manifest.Description,
            };
        }

        // 从注册表中获取当前版本号
        var key = Registry.LocalMachine.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{{{applicationId}}}_is1");
        if (key is null)
        {
            throw new NotInstallCurrectlyException();
        }

        var versionNumber = key.GetValue("DisplayVersion");
        if (versionNumber is null)
        {
            throw new NotInstallCurrectlyException();
        }

        return new ApplicationVersion()
        {
            VersionNumber = versionNumber.ToString()!,
        };
    }

    public static void SetCurrentVersion(Guid applicationId, string versionNumber)
    {
        // 从注册表中获取当前版本号
        var key = Registry.LocalMachine.OpenSubKey(
            $@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{applicationId}_is1",true);
        if (key is null)
        {
            throw new NotInstallCurrectlyException();
        }

        key.SetValue("DisplayVersion", versionNumber);
    }
}