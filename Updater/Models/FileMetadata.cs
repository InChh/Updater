namespace Updater.Models;

public class FileMetadata
{
    public string Path
    {
        get;
        set => field = NormalizePath(value);
    } = string.Empty;

    public string Hash { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Url { get; set; }

    private static string NormalizePath(string path)
    {
        // 统一转换为小写 + 正斜杠
        return path.Replace('\\', '/'); // 替换所有反斜杠
    }
}