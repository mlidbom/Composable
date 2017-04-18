using System;
using JetBrains.Annotations;

namespace Composable.Logging
{

    enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    class ConsoleLogger : ILogger
    {
        readonly Type _type;

        LogLevel logLevel = LogLevel.Info;

        ConsoleLogger(Type type) => _type = type;

        public static ILogger Create(Type type) => new ConsoleLogger(type);
        public void SetLogLevel(LogLevel level) => logLevel = level;
        public void Error(Exception exception, string message)
        {
            if(logLevel >= LogLevel.Error)
            {
                SafeConsole.WriteLine($"ERROR:{_type}: {message} {exception}");
            }
        }

        public void Warning(string message)
        {
            if(logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        public void Warning(Exception exception, string message)
        {
            if(logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
            }
        }

        public void Info(string message)
        {
            if(logLevel >= LogLevel.Info)
            {
                SafeConsole.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }
        public void Debug(string message)
        {
            if(logLevel >= LogLevel.Debug)
            {
                SafeConsole.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        [StringFormatMethod(formatParameterName:"message")]
        public void DebugFormat(string message, params object[] arguments) => Debug(string.Format(message, arguments));
    }
}
