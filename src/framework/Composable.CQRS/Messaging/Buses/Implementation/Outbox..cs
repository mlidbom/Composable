using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox : IOutbox, IDisposable
    {
        class State
        {
            public State(IGlobalBusStateTracker globalBusStateTracker, Router router, RealEndpointConfiguration configuration, IUtcTimeTimeSource timeSource, Outbox.IMessageStorage storage, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                GlobalBusStateTracker = globalBusStateTracker;
                Router = router;
                Configuration = configuration;
                TimeSource = timeSource;
                Storage = storage;
                TypeMapper = typeMapper;
                Serializer = serializer;
            }

            internal bool Running;
            public readonly IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, IInboxConnection> InboxConnections = new Dictionary<EndpointId, IInboxConnection>();
            internal readonly Router Router;
            internal NetMQPoller? Poller;
            public IUtcTimeTimeSource TimeSource { get; }
            public Outbox.IMessageStorage Storage { get; }
            public ITypeMapper TypeMapper { get; }
            public IRemotableMessageSerializer Serializer { get; }
            public readonly RealEndpointConfiguration Configuration;
        }

        readonly IThreadShared<State> _state;
        readonly ITaskRunner _taskRunner;

        public Outbox(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, Outbox.IMessageStorage messageStorage, ITypeMapper typeMapper, RealEndpointConfiguration configuration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
        {
            _taskRunner = taskRunner;
            _state = new OptimizedThreadShared<State>(new State(
                                                          globalBusStateTracker,
                                                          new Router(typeMapper),
                                                          configuration,
                                                          timeSource,
                                                          messageStorage,
                                                          typeMapper,
                                                          serializer));
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            var clientConnection = _state.WithExclusiveAccess(@this => new InboxConnection(@this.GlobalBusStateTracker, remoteEndpoint, @this.Poller!, @this.TimeSource, @this.Storage, @this.TypeMapper, _taskRunner, @this.Serializer));

            await clientConnection.Init().NoMarshalling();

            _state.WithExclusiveAccess(@this =>
            {
                @this.InboxConnections.Add(clientConnection.EndpointInformation.Id, clientConnection);
                @this.Router.AddRegistrations(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
            });
        }

        public async Task StartAsync()
        {
            Task storageStartTask = _state.WithExclusiveAccess(state =>
            {
                Assert.State.Assert(!state.Running);
                state.Running = true;

                storageStartTask = state.Configuration.IsPureClientEndpoint
                                       ? Task.CompletedTask
                                       : state.Storage.StartAsync();

                state.Poller = new NetMQPoller();
                state.Poller.RunAsync($"{nameof(Outbox)}_{nameof(state.Poller)}");
                return storageStartTask;
            });

            await storageStartTask.NoMarshalling();
        }

        public void Stop() => _state.WithExclusiveAccess(state =>
        {
            Assert.State.Assert(state.Running, state.Poller != null, state.Poller.IsRunning);
            state.Running = false;
            state.Poller.StopAsync();
            state.Poller.Dispose();
            state.InboxConnections.Values.ForEach(socket => socket.Dispose());
            state.Poller = null;
        });

        public void PublishIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent) => _state.WithExclusiveAccess(state =>
        {
            var connections = state.Router.SubscriberConnectionsFor(exactlyOnceEvent)
                                   .Where(connection => connection.EndpointInformation.Id != state.Configuration.Id)
                                   .ToArray();//We dispatch events to ourselves synchronously so don't go doing it again here.;

            //Urgent: bug. Our traceability thinking does not allow just discarding this message.But removing this if statement breaks a lot of tests that uses endpoint wiring but do not start an endpoint.
            if(connections.Length != 0)//Don't waste time if there are no receivers
            {
                var eventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
                state.Storage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);
                //Urgent: We should track a Task result here and record the message as being received on success and handle failure.
                Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection => subscriberConnection.Send(exactlyOnceEvent)));
            }
        });

        public void SendIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand) => _state.WithExclusiveAccess(state =>
        {
            var connection = state.Router.ConnectionToHandlerFor(exactlyOnceCommand);
            state.Storage.SaveMessage(exactlyOnceCommand, connection.EndpointInformation.Id);
            //Urgent: We should track a Task result here and record the message as being received on success and handle failure.
            Transaction.Current.OnCommittedSuccessfully(() => connection.Send(exactlyOnceCommand));
        });

        public async Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand atMostOnceCommand)
        {
            IInboxConnection connection = _state.WithExclusiveAccess(state => state.Router.ConnectionToHandlerFor(atMostOnceCommand));

            await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TCommandResult> PostAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> atMostOnceCommand)
        {
            IInboxConnection connection = _state.WithExclusiveAccess(state => state.Router.ConnectionToHandlerFor(atMostOnceCommand));

            return await connection.PostAsync(atMostOnceCommand).NoMarshalling();
        }

        public async Task<TQueryResult> GetAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query)
        {
            var connection = _state.WithExclusiveAccess(state => state.Router.ConnectionToHandlerFor(query));

            return await connection.GetAsync(query).NoMarshalling();
        }

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            if(state.Running)
            {
                Stop();
            }
        });
    }
}
