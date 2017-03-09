namespace Core.Tests
{
  using Composable.System;
  using Composable.Testing;

  using NUnit.Framework;

  [TestFixture]
  public class StrictlyManagedResourceTests
  {
    [Test] public void Allocated_and_disposes_100_instances_in_30_milliseconds()
    {
      TimeAsserter.Execute(() => new StrictlyManagedResource().Dispose(),
        iterations: 100,
        maxTotal: 30.Milliseconds(),
        maxTries: 3);
    }
  }
}