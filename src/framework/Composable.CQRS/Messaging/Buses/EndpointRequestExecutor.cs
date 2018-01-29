using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public static class EndpointRequestExecutor
    {
        //Manual request implementions passed a bus to do their job.
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, Func<IApiBrowser, TResult> request) =>
            @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IApiBrowser>()));


        public static void ExecuteRequestInTransaction(this IEndpoint @this, Action<IApiBrowser> request) => @this.ServiceLocator.ExecuteTransactionInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IApiBrowser>()));
        public static void ExecuteRequest(this IEndpoint @this, Action<IApiBrowser> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IApiBrowser>()));

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, Func<IApiBrowser, Task<TResult>> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScope(async () => await request(endpoint.ServiceLocator.Resolve<IApiBrowser>()));

        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, Func<IApiBrowser, Task> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScope(async () => await request(endpoint.ServiceLocator.Resolve<IApiBrowser>()));

        //Leverage the manual implementations above to enable running navigation specifications as requests
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, RemoteNavigationSpecification<TResult> navigation) => @this.ExecuteRequest(navigation.NavigateOn);
        public static void ExecuteRequest(this IEndpoint @this, RemoteNavigationSpecification navigation) => @this.ExecuteRequest(busSession => navigation.NavigateOn(busSession));
        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, RemoteNavigationSpecification<TResult> navigation) => await endpoint.ExecuteRequestAsync(navigation.NavigateOnAsync);
        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, RemoteNavigationSpecification navigation) => await endpoint.ExecuteRequestAsync(navigation.NavigateOnAsync);

        //Leverage allow for turning it around and access the functionality from the navigation specification instead of from the endpoint. Tastes differ as to which is clearer...
        public static TResult ExecuteAsRequestOn<TResult>(this RemoteNavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);
        public static void ExecuteAsRequestOn(this RemoteNavigationSpecification navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);
        public static async Task<TResult> ExecuteAsRequestOnAsync<TResult>(this RemoteNavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);
        public static async Task ExecuteAsRequestOnAsync(this RemoteNavigationSpecification navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);
    }
}
