using NLog;
using NLog.Config;
using NLog.Targets;
using Sentry;
using Sentry.NLog;
using Logger = NLog.Logger;

namespace FoliCon.Modules.utils;

public static class LogUtils
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public static LoggingConfiguration GetNLogConfig()
    {
        Logger.Info("Getting NLog Configuration");
        LoadConfigurationFromNLog();

        var config = new LoggingConfiguration();
        var logPath = GetLogPath();
        var fileLogLevel = GetFileLogLevel();
  
        var fileTarget = ConfigureFileTarget(logPath);
        var consoleTarget = ConfigureConsoleTarget();

        config.AddRule(fileLogLevel, LogLevel.Fatal, fileTarget);
        config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
    
        Logger.Info("NLog configuration loaded");
        return config;
    }

    private static void LoadConfigurationFromNLog()
    {
        LogManager.Setup().LoadConfigurationFromFile(); 
    }

    private static string GetLogPath()
    {
        return LogManager.Configuration.Variables["logDirectory"].Render(LogEventInfo.CreateNullEvent());
    }

    private static LogLevel GetFileLogLevel()
    {
        return LogLevel.FromString(LogManager.Configuration.Variables["fileLogLevel"].Render(LogEventInfo.CreateNullEvent()));
    }

    private static FileTarget ConfigureFileTarget(string logPath)
    {
        return new FileTarget
        {
            Name = "fileTarget",
            FileName = logPath,
            Layout = LogLayoutString
        };
    }

    private static ColoredConsoleTarget ConfigureConsoleTarget()
    {
        return new ColoredConsoleTarget
        {
            Name = "consoleTarget",
            UseDefaultRowHighlightingRules = true,
            EnableAnsiOutput = true,
            Layout = LogLayoutString
        };
    }

    private const string LogLayoutString = "[${date:format=yyyy-MM-dd HH\\:mm\\:ss.ffff}]-[v${assembly-version:format=Major.Minor.Build}]-${callsite}:${callsite-linenumber}-|${uppercase:${level}}: ${message} ${exception:format=tostring}";


    internal static SentryTarget GetSentryTarget()
    {
        var sentryTarget = new SentryTarget
        {
            Name = "sentry",
            Dsn = Environment.GetEnvironmentVariable("SENTRY_DSN"),
            Layout = "${message} ${exception:format=tostring}",
            BreadcrumbLayout = "${message}",
            Environment = "Development",
            MinimumBreadcrumbLevel = LogLevel.Debug.ToString()!,
            MinimumEventLevel = LogLevel.Error.ToString()!,
            Options =
            {
                SendDefaultPii = true, ShutdownTimeoutSeconds = 5, Debug = false, IsGlobalModeEnabled = true,
                AutoSessionTracking = true,
                User = new SentryNLogUser { Username = Environment.MachineName }
            },
            IncludeEventDataOnBreadcrumbs = true
        };
        sentryTarget.Options.AddExceptionFilterForType<UnauthorizedAccessException>();
        sentryTarget.Tags.Add(new TargetPropertyWithContext("exception", "${exception:format=shorttype}"));
        return sentryTarget;
    }
}