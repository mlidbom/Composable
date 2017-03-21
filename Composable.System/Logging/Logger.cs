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

            static void WriteLine(string message)
            {
                lock(typeof(ConsoleLogger))
                {
                    Console.WriteLine(message);
                }
            }
            public static ILogger Create(Type type) => new ConsoleLogger(type);
            public void Error(string message) => WriteLine($"ERROR:{_type}: {message}");
            public void Warning(string message) => WriteLine($"WARNING:{_type}: {message}");
            public void Info(string message) => WriteLine($"INFO:{_type}: {message}");
            public void Debug(string message) => WriteLine($"DEBUG:{_type}: {message}");
        }
    }
}