﻿using System;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.Logging
{
    static class ConsoleCE
    {
        //NSpec breaks System.Console somehow when tests run in parallel. We are forced to synchronize these tests with other tests and this is the current workaround.
        static readonly MonitorCE Monitor = MonitorCE.WithDefaultTimeout();

        internal static void WriteWarningLine(string message) => WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! {message}");
        internal static void WriteImportantLine(string message) => WriteLine($"############################## {message}");
        internal static void WriteLine(string message) => Monitor.Update(() => Console.WriteLine(message));

        [StringFormatMethod("message")] internal static void WriteLine(string message, params object[] args) =>
            WriteLine(StringCE.FormatInvariant(message, args));

        internal static void WriteLine() =>
            WriteLine("");
    }
}
