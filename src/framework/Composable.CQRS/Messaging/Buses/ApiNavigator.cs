using System;
using System.Threading.Tasks;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    class ApiNavigator : IApiNavigator
    {
        readonly IServiceBus _bus;
        public ApiNavigator(IServiceBus bus) => _bus = bus;

        public IApiNavigator<TCommandResult> Post<TCommandResult>(IDomainCommand<TCommandResult> command)
            => new ApiNavigator<TCommandResult>(_bus, async () => await await _bus.SendAsyncAsync(command));

        public IApiNavigator<TReturnResource> Get<TReturnResource>(IQuery<TReturnResource> query)
            => new ApiNavigator<TReturnResource>(_bus, async () => await _bus.QueryAsync(query));
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

        public IApiNavigator<TReturnResource> Get<TReturnResource>(Func<TCurrentResource, IQuery<TReturnResource>> selectQuery)
            => new ApiNavigator<TReturnResource>(_bus, getCurrentResource: async () => await _bus.QueryAsync(selectQuery(await _getCurrentResource())));

        public IApiNavigator<TReturnResource> Post<TReturnResource>(Func<TCurrentResource, IDomainCommand<TReturnResource>> selectCommand)
            => new ApiNavigator<TReturnResource>(_bus, getCurrentResource: async () => await await _bus.SendAsyncAsync(selectCommand(await _getCurrentResource())));

        public async Task<TCurrentResource> ExecuteAsync() => await _getCurrentResource().NoMarshalling();
    }
}
