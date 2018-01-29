using System;
using Composable.Messaging.Buses.Implementation;
using Composable.Testing;

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

        public static (TException BackendException, MessageDispatchingFailedException FrontEndException) AssertThatRunningScenarioThrowsBackendAndClientTransaction<TException>(this ITestingEndpointHost @this, Action action) where TException : Exception
        {
            var frontEndException = AssertThrows.Exception<MessageDispatchingFailedException>(action);

            return (@this.AssertThrown<TException>(), frontEndException);
        }
    }
}
