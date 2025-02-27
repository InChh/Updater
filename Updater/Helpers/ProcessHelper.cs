using System.Diagnostics;
using System.Linq;

namespace Updater.Helpers;

public static class ProcessHelper
{
    public static bool IsWindowRunning(string windowTitle)
    {
        return Process.GetProcesses().Any(process => process.MainWindowTitle.Contains(windowTitle));
    }
}