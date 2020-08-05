using System;
using Composable.SystemCE;
using JetBrains.Annotations;

namespace Composable.Logging
{
    static class ConsoleCE
    {
        internal static readonly object SynchronizationRoot = typeof(ConsoleCE);


        internal static void WriteWarningLine(string message) => WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {message} !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
        internal static void WriteImportantLine(string message) => WriteLine($"############################## {message} ##############################");
        internal static void WriteLine(string message)
        {
            //NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.
            lock(SynchronizationRoot)
            {
                Console.WriteLine(message);
            }
        }

        [StringFormatMethod("queuedMessageInformation")] internal static void WriteLine(string message, params object[] args) { WriteLine(StringCE.FormatInvariant(message, args)); }

        internal static void WriteLine() => WriteLine("");

        static void Write(string message)
        {
            lock(SynchronizationRoot)
            {
                Console.Write(message);
            }
        }

        [StringFormatMethod("queuedMessageInformation")] internal static void Write(string message, params object[] args) { Write(StringCE.FormatInvariant(message, args)); }
    }
}
