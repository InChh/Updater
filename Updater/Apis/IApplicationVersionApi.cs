using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Updater.Models;
using WebApiClientCore.Attributes;

namespace Updater.Apis;

[LoggingFilter]
public interface IApplicationVersionApi
{
    [HttpGet("application-version/latest/{applicationId}")]
    Task<ApplicationVersion> GetLatestVersionAsync([Required] Guid applicationId);

    [HttpGet("file/by-version-id/{versionId}")]
    Task<List<FileMetadata>> GetFilesByVersionIdAsync([Required] Guid versionId);
}