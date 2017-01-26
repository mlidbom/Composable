using System;
using Composable.System;

namespace CQRS.Tests
{
  using System.Linq;

  using Castle.Core.Internal;

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
        if (IsRunningInNcrunch)
        {
          return ((int)(original.TotalMilliseconds * (NCRunchSlowDownFactor + boost))).Milliseconds();
        }

        if (IsRunningInResharper)
        {
          return ((int)(original.TotalMilliseconds * (ResharperSlowDownFactor + boost))).Milliseconds();
        }
        return original;
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