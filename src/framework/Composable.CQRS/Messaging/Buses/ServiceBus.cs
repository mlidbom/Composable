using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    class ServiceBus : IServiceBus
    {
        readonly Outbox _outbox;

        public ServiceBus(Outbox transport) => _outbox = transport;

        public void SendAtTime(DateTime sendAt, ICommand command) => _outbox.SendAtTime(sendAt, command);

        public void Send(ICommand command) => _outbox.Send(command);

        public void Publish(IEvent anEvent) => _outbox.Publish(anEvent);

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
            => await _outbox.SendAsync(command);

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            =>  await _outbox.QueryAsync(query);

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _outbox.Query(query);

    }
}
