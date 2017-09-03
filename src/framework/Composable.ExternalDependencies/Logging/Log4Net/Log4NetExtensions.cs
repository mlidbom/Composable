using log4net;

namespace Composable.Logging.Log4Net
{
    static class Log4NetExtensions
    {
        // ReSharper disable once UnusedParameter.Global removing the parameter would make it impossible to invoke this as an extension method :)
        internal static ILog Log<T>(this T me) => LogHolder<T>.Logger;

        static class LogHolder<T>
        {
            // ReSharper disable once StaticFieldInGenericType
            public static readonly ILog Logger = LogManager.GetLogger(typeof(T));
        }
    }
}