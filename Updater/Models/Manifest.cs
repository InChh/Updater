using System;
using System.Collections.Generic;

namespace Updater.Models;

public class Manifest
{
    public Guid ApplicationId { get; set; }
    public Guid VersionId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FileMetadata> Files { get; set; } = [];
}