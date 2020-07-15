using System;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging.Hypermedia;

namespace Composable.Messaging.Buses
{
    public static class EndpointRequestExecutor
    {
        //Manual request implementions passed a bus to do their job.
        public static TResult ExecuteServerRequest<TResult>(this IEndpoint @this, Func<IServiceBusSession, TResult> request) =>
            @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));


        public static void ExecuteServerRequestInTransaction(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteTransactionInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));


        public static async Task<TResult> ExecuteServerRequestAsync<TResult>(this IEndpoint endpoint, Func<IServiceBusSession, Task<TResult>> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(endpoint.ServiceLocator.Resolve<IServiceBusSession>()));


        public static void ExecuteServerRequest(this IEndpoint @this, Action<IServiceBusSession> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IServiceBusSession>()));


        //Urgent: Most of these ExecuteClientRequests are very suspect. Are they really endpoint actions or something about pure clients?
        public static void ExecuteClientRequest(this IEndpoint @this, Action<IRemoteHypermediaNavigator> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));
        public static TResult ExecuteClientRequest<TResult>(this IEndpoint @this, Func<IRemoteHypermediaNavigator, TResult> request) => @this.ServiceLocator.ExecuteInIsolatedScope(() => request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));
        public static async Task<TResult> ExecuteClientRequestAsync<TResult>(this IEndpoint @this, Func<IRemoteHypermediaNavigator, Task<TResult>> request) =>
            await @this.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(@this.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

        public static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, Func<Task> request) =>await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request());

        public static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, Func<IRemoteHypermediaNavigator, Task> request) =>
            await endpoint.ServiceLocator.ExecuteInIsolatedScopeAsync(async () => await request(endpoint.ServiceLocator.Resolve<IRemoteHypermediaNavigator>()));

        //Leverage the manual implementations above to enable running navigation specifications as requests
        public static TResult ExecuteClientRequest<TResult>(this IEndpoint @this, NavigationSpecification<TResult> navigation) => @this.ExecuteClientRequest(navigation.NavigateOn);
        public static void ExecuteClientRequest(this IEndpoint @this, NavigationSpecification navigation) => @this.ExecuteClientRequest(navigation.NavigateOn);
        public static async Task<TResult> ExecuteRequestAsync<TResult>(this IEndpoint endpoint, NavigationSpecification<TResult> navigation) => await endpoint.ExecuteClientRequestAsync(navigation.NavigateOnAsync);
        public static async Task ExecuteClientRequestAsync(this IEndpoint endpoint, NavigationSpecification navigation) => await endpoint.ExecuteClientRequestAsync(navigation.NavigateOnAsync);

        //Leverage allow for turning it around and access the functionality from the navigation specification instead of from the endpoint. Tastes differ as to which is clearer...
        public static TResult ExecuteAsClientRequestOn<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteClientRequest(navigationSpecification);
        public static void ExecuteAsClientRequestOn(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => endpoint.ExecuteClientRequest(navigationSpecification);
        public static async Task<TResult> ExecuteAsClientRequestOnAsync<TResult>(this NavigationSpecification<TResult> navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteRequestAsync(navigationSpecification);
        public static async Task ExecuteAsClientRequestOnAsync(this NavigationSpecification navigationSpecification, IEndpoint endpoint) => await endpoint.ExecuteClientRequestAsync(navigationSpecification);
    }
}
