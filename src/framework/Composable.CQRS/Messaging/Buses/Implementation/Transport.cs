using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Transport : ITransport, IDisposable
    {
        readonly IGlobalBusStateTracker _globalBusStateTracker;
        readonly ITypeMapper _typeMapper;
#pragma warning disable CA2213 // Disposable fields should be disposed: This is a bug in the analyzer.
        readonly NetMQPoller _poller;
#pragma warning restore CA2213 // Disposable fields should be disposed
        readonly IRemotableMessageSerializer _serializer;

        bool _running;
        readonly Router _router;
        IReadOnlyDictionary<EndpointId, IInboxConnection> _inboxConnections = new Dictionary<EndpointId, IInboxConnection>();
        readonly AssertAndRun _runningAndNotDisposed;

        public Transport(IGlobalBusStateTracker globalBusStateTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
        {
            _runningAndNotDisposed = new AssertAndRun(() => Assert.State.Assert(_running, _poller != null, _poller.IsRunning));
            _router = new Router(typeMapper);
            _serializer = serializer;
            _globalBusStateTracker = globalBusStateTracker;
            _typeMapper = typeMapper;

            _poller = new NetMQPoller();
            _poller.RunAsync($"{nameof(Outbox)}_{nameof(NetMQPoller)}");
            _running = true;
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            _runningAndNotDisposed.Assert();
            var clientConnection = new Outbox.InboxConnection(_globalBusStateTracker, remoteEndpoint, _poller!, _typeMapper, _serializer);

            await clientConnection.Init().NoMarshalling();

            ThreadSafe.AddToCopyAndReplace(ref _inboxConnections, clientConnection.EndpointInformation.Id, clientConnection);

            _router.RegisterRoutes(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
        }

        public IInboxConnection ConnectionToHandlerFor(MessageTypes.IRemotableCommand command) =>
            _runningAndNotDisposed.Do(() => _router.ConnectionToHandlerFor(command));

        public IReadOnlyList<IInboxConnection> SubscriberConnectionsFor(MessageTypes.IExactlyOnceEvent @event) =>
            _runningAndNotDisposed.Do(() => _router.SubscriberConnectionsFor(@event));

        public async Task PostAsync(MessageTypes.IAtMostOnceHypermediaCommand atMostOnceCommand)
        {
            _runningAndNotDisposed.Assert();
            var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
            await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.IAtMostOnceCommand<TCommandResult> atMostOnceCommand)
        {
            _runningAndNotDisposed.Assert();
            var connection = _router.ConnectionToHandlerFor(atMostOnceCommand);
            return await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.IRemotableQuery<TQueryResult> query)
        {
            _runningAndNotDisposed.Assert();
            var connection = _router.ConnectionToHandlerFor(query);
            return await connection.GetAsync(query).NoMarshalling();
        }

        public void Stop() => _runningAndNotDisposed.Do(() =>
        {
            _running = false;
            _poller.StopAsync();
        });

        bool _disposed;
        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                if(_running)
                {
                    Stop();
                }
                _poller.Dispose();
                _inboxConnections.Values.DisposeAll();
            }
        }
    }
}
