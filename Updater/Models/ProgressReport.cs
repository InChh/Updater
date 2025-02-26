namespace Updater.Models;

public class ProgressReport
{
    public string Phase { get; set; } = string.Empty; // 当前阶段描述
    public int TotalSteps { get; set; } // 总步骤数
    public int CurrentStep { get; set; } // 当前步骤序号
    public double ProgressPercentage => (CurrentStep / (double)TotalSteps) * 100;

    // 详细进度信息
    public string CurrentFile { get; set; } = string.Empty;
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
}