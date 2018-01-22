using System;
using System.Threading.Tasks;
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

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint @this, Func<IServiceBusSession, Task<TResult>> request)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                return await request(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static async Task ExecuteRequestAsync(this IEndpoint @this, Func<IServiceBusSession, Task> request)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                await request(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                return navigation.ExecuteOn(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static void ExecuteRequest(this IEndpoint @this, NavigationSpecification navigation)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                navigation.ExecuteOn(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                return await navigation.ExecuteAsyncOn(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }

        public static async Task ExecuteRequestAsync(this IEndpoint @this, NavigationSpecification request)
        {
            using(@this.ServiceLocator.BeginScope())
            {
                await request.ExecuteAsyncOn(@this.ServiceLocator.Resolve<IServiceBusSession>());
            }
        }
    }
}
