using NUnit.Framework;

namespace Composable.Tests
{
    class SlowAttribute : CategoryAttribute
    {
        public SlowAttribute() : base("Slow")
        {
        }
    }

    class PerformanceAttribute : CategoryAttribute
    {
        public PerformanceAttribute() : base("Performance")
        {
        }
    }
}