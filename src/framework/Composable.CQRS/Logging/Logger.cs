using System;
using JetBrains.Annotations;

// ReSharper disable UnusedMember.Global

namespace Composable.Logging
{
    interface ILogger
    {
        ILogger WithLogLevel(LogLevel level);
        void Error(Exception exception, string message = null);
        void Warning(string message);
        void Warning(Exception exception, string message);
        void Info(string message);
        void Debug(string message);
        [StringFormatMethod(formatParameterName: "queuedMessageInformation")]
        void DebugFormat(string message, params object[] arguments);
    }

    static class Logger
    {
        internal static ILogger For<T>() => LogCache<T>.Logger;
        internal static ILogger For(Type loggingType) => LoggerFactoryMethod(loggingType);

        static readonly Func<Type, ILogger> LoggerFactoryMethod = ConsoleLogger.Create;
        static class LogCache<T>
        {
            // ReSharper disable once StaticFieldInGenericType
            public static readonly ILogger Logger = LoggerFactoryMethod(typeof(T));
        }
    }
}