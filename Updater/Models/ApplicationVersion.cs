using System;

namespace Updater.Models;

public class ApplicationVersion
{
    public Guid Id { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}