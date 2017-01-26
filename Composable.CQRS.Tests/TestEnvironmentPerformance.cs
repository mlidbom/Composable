using System;
using Composable.System;
using System.Linq;

namespace CQRS.Tests
{
  public class TestEnvironment
  {
    public static TestRunner TestRunner = TestRunner.Instance;
  }

  public class TestRunner
  {
    public static readonly TestRunner Instance;

    static TestRunner()
    {
      if (IsRunningInNcrunch)
      {
        Instance = new TestRunner("NCRunch", 5.0);
      } else if (IsRunningInResharper)
      {
        Instance = new TestRunner("Resharper", 2.0);
      }
      else
      {
        Instance = new TestRunner("Default/Fallback", 1);
      }
    }

    private static bool IsRunningInNcrunch => NCrunch.Framework.NCrunchEnvironment.NCrunchIsResident();

    private static readonly bool IsRunningInResharper = AreWeRunningInResharper();

    private static bool AreWeRunningInResharper()
    {
      return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName.StartsWith("JetBrains.ReSharper.UnitTestRunner"));
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

  public static class TestEnvironmentPerformance
  {

        private static bool IsRunningInNcrunch => NCrunch.Framework.NCrunchEnvironment.NCrunchIsResident();

      private static readonly bool IsRunningInResharper = AreWeRunningInResharper();

      private static bool AreWeRunningInResharper()
      {
        return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.FullName.StartsWith("JetBrains.ReSharper.UnitTestRunner"));
      }

      private static double NCRunchSlowDownFactor = 5.0;
      private static double ResharperSlowDownFactor = 2.0;

    //todo: Detect and adjust the abilities of the running machine and adjust expected runtime accordingly.
    public static TimeSpan AdjustRuntime(TimeSpan original, double boost = 1.0)
      {
          return ((int)(original.TotalMilliseconds * (TestRunner.Instance.SlowDownFactor + boost))).Milliseconds();
      }

      public static int AdjustIterations(int original, double boost = 1.0)
        {
            if (IsRunningInNcrunch)
            {
                return (int)(original / (NCRunchSlowDownFactor + boost));
            }
            return original;
        }
    }

    public static class NCrunchPerformanceExtensions
    {
        
        public static TimeSpan AdjustRuntimeToTestRunner(this TimeSpan @this, double boost = 1.0) => TestEnvironmentPerformance.AdjustRuntime(@this, boost);        
        public static int AdjustIterationsForNCrunch(this int @this, double boost = 1.0) => TestEnvironmentPerformance.AdjustIterations(@this, boost);
    }
}