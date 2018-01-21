using System;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public static class EndpointExtensions
    {
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, Func<IServiceBusSession, TResult> request)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                return request(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static void ExecuteRequest(this IEndpoint @this, Action<IServiceBusSession> request)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                request(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }
    }
}
