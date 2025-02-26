using System;

namespace Updater.Models;

public class CheckUpdateResult
{
    public bool HasNewVersion { get; }
    public ApplicationVersion CurrentVersion { get; }

    public CheckUpdateResult(bool hasNewVersion,
        ApplicationVersion currentVersion,
        ApplicationVersion? latestVersion = null)
    {
        HasNewVersion = hasNewVersion;
        CurrentVersion = currentVersion;
        if (hasNewVersion && latestVersion is null)
        {
            throw new ArgumentNullException(nameof(latestVersion));
        }

        LatestVersion = latestVersion;
    }

    public ApplicationVersion? LatestVersion { get; }
}