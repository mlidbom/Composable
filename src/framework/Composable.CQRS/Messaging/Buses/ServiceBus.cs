using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;

namespace Composable.Messaging.Buses
{
    partial class ServiceBus : IServiceBus
    {
        readonly InterprocessTransport _transport;


        public IReadOnlyList<Exception> ThrownExceptions => _transport.ThrownExceptions;

        public ServiceBus(InterprocessTransport transport) => _transport = transport;

        public void SendAtTime(DateTime sendAt, ICommand command) => _transport.SendAtTime(sendAt, command);

        public void Send(ICommand command) => _transport.Send(command);

        public void Publish(IEvent anEvent) => _transport.Publish(anEvent);

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
            => await _transport.SendAsync(command);

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            =>  await _transport.QueryAsync(query);

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult
            => _transport.Query(query);

    }
}
