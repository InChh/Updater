using System.Collections.Generic;

namespace Updater.Models;

public class DiffResult
{
    // 需要更新的文件列表
    public List<FileMetadata> FilesToUpdate { get; set; } = [];

    // 需要下载并添加的文件列表
    public List<FileMetadata> FilesToAdd { get; set; } = [];

    // 需要重命名的文件列表，key 为原文件路径，value 为新文件路径
    public Dictionary<string, string> FilesToRename { get; set; } = new();

    // 需要删除的文件路径列表
    public List<string> FilesToDelete { get; set; } = [];
}