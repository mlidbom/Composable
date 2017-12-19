using System;
using JetBrains.Annotations;

namespace Composable.Logging
{
    ///<summary>This class exists mostly because NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.</summary>
    static class SafeConsole
    {
        internal static readonly object SynchronizationRoot = typeof(SafeConsole);
        internal static void WriteLine(string message)
        {
            lock(SynchronizationRoot)
            {
                Console.WriteLine(message);
            }
        }

        [StringFormatMethod("queuedMessageInformation")] internal static void WriteLine(string message, params object[] args) { WriteLine(string.Format(message, args)); }

        internal static void WriteLine() => WriteLine("");

        static void Write(string message)
        {
            lock(SynchronizationRoot)
            {
                Console.Write(message);
            }
        }

        [StringFormatMethod("queuedMessageInformation")] internal static void Write(string message, params object[] args) { Write(string.Format(message, args)); }
    }
}
