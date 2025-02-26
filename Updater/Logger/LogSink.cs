using System.Linq;
using Avalonia.Logging;
using Serilog;

namespace Updater.Logger;

public class LogSink : ILogSink
{
    private readonly LogEventLevel _level;
    private readonly string[] _areas;

    public LogSink(LogEventLevel level,
        params string[] areas)
    {
        _level = level;
        _areas = areas;
        Serilog.Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Is(ConvertLogLevel(level))
            .MinimumLevel.Override("Microsoft", ConvertLogLevel(LogEventLevel.Information))
            .WriteTo.Console()
            .WriteTo.File("logs/UpdaterLog.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    public bool IsEnabled(LogEventLevel l, string area)
    {
        return _level >= l && (_areas.Length == 0 || _areas.Contains(area));
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Serilog.Log.Write(ConvertLogLevel(level), messageTemplate);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate,
        params object?[] propertyValues)
    {
        Serilog.Log.Write(ConvertLogLevel(level), messageTemplate, propertyValues);
    }

    private static Serilog.Events.LogEventLevel ConvertLogLevel(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
            LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
            LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
            LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
            LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
            _ => Serilog.Events.LogEventLevel.Verbose
        };
    }
}