using System;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    public static class TestingHostExtensions
    {
        public static TException AssertThatRunningScenarioThrowsBackendException<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch(AggregateException exception) when(exception.InnerException is MessageDispatchingFailedException) {}

            return @this.AssertThrown<TException>();
        }
    }
}
