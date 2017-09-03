using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Composable.Logging;
using Composable.System;

namespace Composable.Testing.Performance
{
    static class TestEnvironment
    {
        public static TimeSpan AdjustRuntimeToTestEnvironment(this TimeSpan original) => ((int)(original.TotalMilliseconds * TestRunner.Instance.SlowDownFactor)).Milliseconds();
    }

    class TestRunner
    {
        public static readonly TestRunner Instance = GetInstance();
        static readonly ILogger Log = Logger.For<TestRunner>();

        static TestRunner GetInstance()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var processName = Process.GetCurrentProcess().ProcessName;
            if (loadedAssemblies.Any(assembly => assembly.FullName.StartsWith("nCrunch.TaskRunner")))
            {
                return new TestRunner("NCRunch", 5.0);
            }

            if (processName.StartsWith("nunit-gui"))
            {
                return new TestRunner("Nunit GUI");
            }

            if (processName.StartsWith("vstest"))
            {
                return new TestRunner("Visual Studio (vstest)");
            }

            if (processName.StartsWith("nunit-console"))
            {
                return new TestRunner("Nunit Console");
            }

            if (AreWeRunningInResharper(loadedAssemblies))
            {
                return new TestRunner("Resharper");
            }


            Log.Debug(processName);
            loadedAssemblies.ForEach(assembly =>  Log.Debug(assembly.FullName));

            return new TestRunner($"Default/Fallback ({processName})");
        }

        static bool AreWeRunningInResharper(IEnumerable<Assembly> loadedAssemblies)
        {
            return loadedAssemblies.Any(assembly => assembly.FullName.StartsWith("JetBrains.ReSharper.UnitTestRunner"));
        }

        TestRunner(string name, double slowDownFactor = 1.0)
        {
            SafeConsole.WriteLine($"Setting up performance adjustments for {name} with {nameof(slowDownFactor)}: {slowDownFactor}");
            SlowDownFactor = slowDownFactor;
        }

        public double SlowDownFactor { get; }
    }
}
