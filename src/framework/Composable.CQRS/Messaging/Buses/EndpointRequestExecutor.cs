using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public static class EndpointRequestExecutor
    {
        //Manual request implementions passed a bus to do their job.
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

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, Func<IServiceBusSession, Task<TResult>> request)
        {
            using(endpoint.ServiceLocator.BeginScope())
            {
                return await request(endpoint.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, Func<IServiceBusSession, Task> request)
        {
            using(endpoint.ServiceLocator.BeginScope())
            {
                await request(endpoint.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation) => @this.ExecuteRequest(navigation.ExecuteOn);

        public static void ExecuteRequest(this IEndpoint @this, NavigationSpecification navigation) => @this.ExecuteRequest(navigation.ExecuteOn);

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, NavigationSpecification<TResult> navigation) => await endpoint.ExecuteRequestAsync(navigation.ExecuteAsyncOn);

        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, NavigationSpecification navigation)  => await endpoint.ExecuteRequestAsync(navigation.ExecuteAsyncOn);


        public static TResult ExecuteAsRequestOn<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);

        public static void ExecuteAsRequestOn(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);

        public static async Task<TResult> ExecuteAsRequestOnAsync<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);

        public static async Task ExecuteAsRequestOnAsync(this NavigationSpecification navigationSpecification, IEndpoint endpoint)  => await endpoint.ExecuteRequestAsync(navigationSpecification);
    }
}
