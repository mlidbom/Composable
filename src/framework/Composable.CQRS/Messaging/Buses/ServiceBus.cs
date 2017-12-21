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

        public void SendAtTime(DateTime sendAt, IDomainCommand command) => _outbox.SendAtTime(sendAt, command);

        public void Send(IDomainCommand command)
        {
            CommandValidator.AssertCommandIsValid(command);
            _outbox.Send(command);
        }

        public void Publish(IEvent anEvent) => _outbox.Publish(anEvent);

        public async Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command)
        {
            CommandValidator.AssertCommandIsValid(command);
            return await _outbox.SendAsync(command).NoMarshalling();
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query)
            =>  await _outbox.QueryAsync(query).NoMarshalling();

        public TResult Query<TResult>(IQuery<TResult> query)
            => _outbox.Query(query);

    }
}
