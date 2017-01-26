using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Composable.System;

namespace CQRS.Tests
{
    public static class TestEnvironment
    {
        public static TimeSpan AdjustRuntimeToTestEnvironment(this TimeSpan original, double boost = 0)
        {
            return ((int)(original.TotalMilliseconds * (TestRunner.Instance.SlowDownFactor + boost))).Milliseconds();
        }
    }

    class TestRunner
    {
        public static readonly TestRunner Instance = GetInstance();

        private static TestRunner GetInstance()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var processName = Process.GetCurrentProcess().ProcessName;
            if (loadedAssemblies.Any(assembly => assembly.FullName.StartsWith("nCrunch.TaskRunner")))
            {
                return new TestRunner("NCRunch", 5.0);
            }

            if (processName.StartsWith("nunit-gui"))
            {
                return new TestRunner("Nunit GUI", 1);
            }

            if (processName.StartsWith("vstest"))
            {
                return new TestRunner("Visual Studio (vstest)", 1.0);
            }

            if (processName.StartsWith("nunit-console"))
            {
                return new TestRunner("Nunit Console", 1);
            }

            if (AreWeRunningInResharper(loadedAssemblies))
            {
                return new TestRunner("Resharper", 2.0);
            }


            Console.WriteLine(processName);
            loadedAssemblies.ForEach(Console.WriteLine);

            return new TestRunner($"Default/Fallback ({processName})", 1);
        }

        private static bool AreWeRunningInResharper(IEnumerable<Assembly> loadedAssemblies)
        {
            return loadedAssemblies.Any(assembly => assembly.FullName.StartsWith("JetBrains.ReSharper.UnitTestRunner"));
        }

        TestRunner(string name, double slowDownFactor)
        {
            Console.WriteLine($"Setting up performance adjustments for {name} with {nameof(slowDownFactor)}: {slowDownFactor}");
            Name = name;
            SlowDownFactor = slowDownFactor;
        }

        public string Name { get; }
        public double SlowDownFactor { get; }
    }
}
