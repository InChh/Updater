using System;

namespace Updater.Models;

public class Config
{
    public string ServerUrl { get; init; } = string.Empty;
    public Guid ApplicationId { get; init; }
    public string OssEndpoint { get; init; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}