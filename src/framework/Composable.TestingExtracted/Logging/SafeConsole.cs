using System;

namespace Composable.Testing.Logging
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
    }
}
