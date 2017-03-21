using NUnit.Framework;

namespace Composable.CQRS.Tests
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