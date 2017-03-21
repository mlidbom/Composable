using System;

namespace Composable.Logging
{
    interface ILogger
    {
        void Error(string message);
        void Warning(string message);
        void Info(string message);
        void Debug(string message);
    }

    static class Logger
    {
        internal static ILogger For<T>() => LogCache<T>.Logger;
        internal static ILogger For(Type loggingType) => LoggerFactoryMethod(loggingType);
        internal static ILogger Log<T>(this T @this) => LogCache<T>.Logger;

        static readonly Func<Type, ILogger> LoggerFactoryMethod = ConsoleLogger.Create;
        static class LogCache<T>
        {
            // ReSharper disable once StaticFieldInGenericType
            public static readonly ILogger Logger = LoggerFactoryMethod(typeof(T));
        }

        class ConsoleLogger : ILogger
        {
            readonly Type _type;

            ConsoleLogger(Type type) => _type = type;

            public static ILogger Create(Type type) => new ConsoleLogger(type);
            public void Error(string message) => SafeConsole.WriteLine($"ERROR:{_type}: {message}");
            public void Warning(string message) => SafeConsole.WriteLine($"WARNING:{_type}: {message}");
            public void Info(string message) => SafeConsole.WriteLine($"INFO:{_type}: {message}");
            public void Debug(string message) => SafeConsole.WriteLine($"DEBUG:{_type}: {message}");
        }
    }

    static class SafeConsole
    {
        internal static readonly object SyncronizationRoot = typeof(SafeConsole);
        internal static void WriteLine(string message)
        {
            lock (SyncronizationRoot)
            {
                Console.WriteLine(message);
            }
        }
    }
}