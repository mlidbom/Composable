using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    public static class EndpointRequestExecutor
    {
        //Manual request implementions passed a bus to do their job.
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, Func<IServiceBusSession, TResult> request) =>
            @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));


        public static void ExecuteRequestInTransaction(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteTransactionInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));
        public static void ExecuteRequest(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));

        public static void ExecuteRequest(this IEndpoint @this, Action<IRemoteApiNavigatorSession> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteApiNavigatorSession>()));
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, Func<IRemoteApiNavigatorSession, TResult> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteApiNavigatorSession>()));
        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint @this, Func<IRemoteApiNavigatorSession, Task<TResult>> request) =>
            await @this.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(@this.ServiceLocator.Resolve<IRemoteApiNavigatorSession>()));

        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, Func<IServiceBusSession, Task<TResult>> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(endpoint.ServiceLocator.Resolve<IServiceBusSession>()));

        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, Func<Task> request) =>await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request());

        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, Func<IServiceBusSession, Task> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(endpoint.ServiceLocator.Resolve<IServiceBusSession>()));

        //Leverage the manual implementations above to enable running navigation specifications as requests
        public static TResult ExecuteRequest<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation) => @this.ExecuteRequest(navigation.NavigateOn);
        public static void ExecuteRequest(this IEndpoint @this, NavigationSpecification navigation) => @this.ExecuteRequest(navigation.NavigateOn);
        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, NavigationSpecification<TResult> navigation) => await endpoint.ExecuteRequestAsync(navigation.NavigateOnAsync);
        public static async Task ExecuteRequestAsync(this IEndpoint endpoint, NavigationSpecification navigation) => await endpoint.ExecuteRequestAsync(navigation.NavigateOnAsync);

        //Leverage allow for turning it around and access the functionality from the navigation specification instead of from the endpoint. Tastes differ as to which is clearer...
        public static TResult ExecuteAsRequestOn<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);
        public static void ExecuteAsRequestOn(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteRequest(navigationSpecification);
        public static async Task<TResult> ExecuteAsRequestOnAsync<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);
        public static async Task ExecuteAsRequestOnAsync(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);
    }
}
