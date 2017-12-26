using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly Outbox _outbox;

        public ServiceBus(Outbox transport) => _outbox = transport;

        public Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command) => _outbox.SendAtTimeAsync(sendAt, command);

        public Task SendAsync(IDomainCommand command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return _outbox.SendAsync(command);
        }

        public Task PublishAsync(IEvent anEvent) => _outbox.PublishAsync(anEvent);

        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return await _outbox.SendAsync(command).NoMarshalling();
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
            =>  await _outbox.QueryAsync(query).NoMarshalling();

        public TResult Query<TResult>(IQuery<TResult> query)
            => QueryAsync(query).Result;

    }
}
