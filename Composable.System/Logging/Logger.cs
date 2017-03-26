using System;
// ReSharper disable UnusedMember.Global

namespace Composable.Logging
{
    interface ILogger
    {
        void Error(Exception exception, string message = null);
        void Warning(string message);
        void Info(string message);
        void Debug(string message);
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