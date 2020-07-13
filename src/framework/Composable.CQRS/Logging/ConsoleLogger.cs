﻿using System;
using Composable.System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;

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

        LogLevel _logLevel = LogLevel.Info;

        ConsoleLogger(Type type) => _type = type;

        public static ILogger Create(Type type) => new ConsoleLogger(type);
        public ILogger WithLogLevel(LogLevel level) => new ConsoleLogger(_type){_logLevel =  level};
        public void Error(Exception exception, string? message)
        {
            if(_logLevel >= LogLevel.Error)
            {
                SafeConsole.WriteLine($@"
############################################# ERROR in : {_type.GetFullNameCompilable()} #############################################
MESSAGE: {message} 
EXCEPTION: {exception}
############################################# SERIALIZED EXCEPTION #############################################
{SerializeException(exception)}
############################################# END ERROR #############################################
");
            }
        }


        static string SerializeException(Exception exception)
        {
            try
            {
                return JsonConvert.SerializeObject(exception, Formatting.Indented);
            }
            catch(Exception e)
            {
                return $"Serialization Failed with message: {e.Message}";
            }
        }

        public void Warning(string message)
        {
            if(_logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        public void Warning(Exception exception, string message)
        {
            if(_logLevel >= LogLevel.Warning)
            {
                SafeConsole.WriteLine($"WARNING:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}, \n: Exception: {exception}");
            }
        }

        public void Info(string message)
        {
            if(_logLevel >= LogLevel.Info)
            {
                SafeConsole.WriteLine($"INFO:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }
        public void Debug(string message)
        {
            if(_logLevel >= LogLevel.Debug)
            {
                SafeConsole.WriteLine($"DEBUG:{_type}: {DateTime.Now:HH:mm:ss.fff} {message}");
            }
        }

        [StringFormatMethod(formatParameterName:"queuedMessageInformation")]
        public void DebugFormat(string message, params object[] arguments) => Debug(string.Format(message, arguments));
    }
}
