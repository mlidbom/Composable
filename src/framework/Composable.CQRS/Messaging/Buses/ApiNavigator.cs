using System;
using System.Threading.Tasks;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    class ApiNavigator : IApiNavigator
    {
        readonly IServiceBus _bus;
        public ApiNavigator(IServiceBus bus) => _bus = bus;

        public IApiNavigator Execute(ICommand command)
        {
            _bus.Send(command);
            return this;
        }

        public IApiNavigator<TCommandResult> Execute<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
            => new ApiNavigator<TCommandResult>(_bus, () => _bus.SendAsync(command));


        public IApiNavigator<TReturnResource> Get<TReturnResource>(IQuery<TReturnResource> query) where TReturnResource : IQueryResult
            => new ApiNavigator<TReturnResource>(_bus, () => _bus.QueryAsync(query));
    }

    class ApiNavigator<TCurrentResource> : IApiNavigator<TCurrentResource>
    {
        readonly IServiceBus _bus;
        readonly Func<Task<TCurrentResource>> _getCurrentResource;

        public ApiNavigator(IServiceBus bus, Func<Task<TCurrentResource>> getCurrentResource)
        {
            _bus = bus;
            _getCurrentResource = getCurrentResource;
        }

        public IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery) where TReturnResource : IQueryResult
            => new ApiNavigator<TReturnResource>(_bus, () => _bus.QueryAsync(selectQuery(_getCurrentResource().Result)));

        public IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, ICommand<TReturnResource>> selectCommand) where TReturnResource : IMessage
            => new ApiNavigator<TReturnResource>(_bus, () => _bus.SendAsync(selectCommand(_getCurrentResource().Result)));

        public async Task<TCurrentResource> ExecuteNavigationAsync() => await _getCurrentResource().IgnoreSynchronizationContext();

        public TCurrentResource ExecuteNavigation() => ExecuteNavigationAsync().Result;
    }
}
