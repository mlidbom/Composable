using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly IOutbox _outbox;

        public ServiceBus(IOutbox transport) => _outbox = transport;

        public async Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command) => await _outbox.SendAtTimeAsync(sendAt, command).NoMarshalling();

        public async Task SendAsync(IDomainCommand command)
        {
            CommandValidator.AssertCommandIsValid(command);
            await _outbox.SendAsync(command).NoMarshalling();
        }

        public async Task PublishAsync(IEvent anEvent) => await _outbox.PublishAsync(anEvent).NoMarshalling();

        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return await _outbox.SendAsync(command).NoMarshalling();
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
            =>  await _outbox.QueryAsync(query).NoMarshalling();

    }
}
