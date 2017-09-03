using System;
using JetBrains.Annotations;

namespace Composable.Logging
{
    ///<summary>This class exists mostly because nspec breaks System.Console somehow when tests run in paralell. We are forced to syncronize these tests with other tests and this is the current workaround.</summary>
    static class SafeConsole
    {
        internal static readonly object SyncronizationRoot = typeof(SafeConsole);
        internal static void WriteLine(string message)
        {
            lock(SyncronizationRoot)
            {
                Console.WriteLine(message);
            }
        }

        [StringFormatMethod("message")] internal static void WriteLine(string message, params object[] args) { WriteLine(string.Format(message, args)); }

        internal static void WriteLine() => WriteLine("");

        static void Write(string message)
        {
            lock(SyncronizationRoot)
            {
                Console.Write(message);
            }
        }

        [StringFormatMethod("message")] internal static void Write(string message, params object[] args) { Write(string.Format(message, args)); }
    }
}
