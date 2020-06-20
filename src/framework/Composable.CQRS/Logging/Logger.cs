using System;
using JetBrains.Annotations;

// ReSharper disable UnusedMember.Global

namespace Composable.Logging
{
    interface ILogger
    {
        ILogger WithLogLevel(LogLevel level);
        void Error(Exception exception, string? message = null);
        void Warning(string message);
        void Warning(Exception exception, string message);
        void Info(string message);
        void Debug(string message);
        [StringFormatMethod(formatParameterName: "queuedMessageInformation")]
        void DebugFormat(string message, params object[] arguments);
    }

    static class Logger
    {
        // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
#pragma warning disable IDE0060 //Review OK: removing the parameter would make it impossible to invoke this as an extension method :)
        internal static ILogger Log<T>(this T me) => LogCache<T>.Logger;
#pragma warning restore IDE0060 // Remove unused parameter

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