using System;
using System.Threading.Tasks;
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

        public static async Task<TException> AssertThatRunningScenarioThrowsBackendExceptionAsync<TException>(this ITestingEndpointHost @this, Func<Task> action) where TException : Exception
        {
            try
            {
                await action();
            }
            catch(AggregateException exception) when(exception.InnerException is MessageDispatchingFailedException) {}

            return @this.AssertThrown<TException>();
        }
    }
}
