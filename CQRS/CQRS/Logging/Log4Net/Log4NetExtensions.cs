using log4net;

namespace Composable.Logging.Log4Net
{
    public static class Log4NetExtensions
    {
        public static ILog Log<T>(this T me)
        {
            return LogHolder<T>.Logger;
        }

        private static class LogHolder<T>
        {
            // ReSharper disable once StaticFieldInGenericType
            public static readonly ILog Logger = LogManager.GetLogger(typeof(T));
        }
    }
}