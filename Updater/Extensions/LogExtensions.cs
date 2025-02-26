using Avalonia;
using Avalonia.Logging;
using Updater.Logger;

namespace Updater.Extensions;

public static class LogExtensions
{
    public static AppBuilder LogToMySink(this AppBuilder builder,
        LogEventLevel level = LogEventLevel.Warning,
        params string[] areas)
    {
        Avalonia.Logging.Logger.Sink = new LogSink(level, areas);
        return builder;
    }
}