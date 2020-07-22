using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
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
            public State(IGlobalBusStateTracker globalBusStateTracker, Router router, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                GlobalBusStateTracker = globalBusStateTracker;
                Router = router;
                Configuration = configuration;
                TypeMapper = typeMapper;
                Serializer = serializer;
            }

            internal bool Running;
            public readonly IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, IInboxConnection> InboxConnections = new Dictionary<EndpointId, IInboxConnection>();
            internal readonly Router Router;
            internal NetMQPoller? Poller;
            public ITypeMapper TypeMapper { get; }
            public IRemotableMessageSerializer Serializer { get; }
            public readonly RealEndpointConfiguration Configuration;
        }

        readonly IThreadShared<State> _state;
        readonly Outbox.IMessageStorage _storage;

        public Outbox(IGlobalBusStateTracker globalBusStateTracker, Outbox.IMessageStorage messageStorage, ITypeMapper typeMapper, RealEndpointConfiguration configuration, IRemotableMessageSerializer serializer)
        {
            _storage = messageStorage;
            _state = new OptimizedThreadShared<State>(new State(
                                                          globalBusStateTracker,
                                                          new Router(typeMapper),
                                                          configuration,
                                                          typeMapper,
                                                          serializer));
        }

        public async Task ConnectAsync(EndPointAddress remoteEndpoint)
        {
            var clientConnection = _state.WithExclusiveAccess(@this => new InboxConnection(@this.GlobalBusStateTracker, remoteEndpoint, @this.Poller!, @this.TypeMapper, @this.Serializer));

            await clientConnection.Init().NoMarshalling();

            _state.WithExclusiveAccess(@this =>
            {
                @this.InboxConnections.Add(clientConnection.EndpointInformation.Id, clientConnection);
                @this.Router.AddRegistrations(clientConnection, clientConnection.EndpointInformation.HandledMessageTypes);
            });
        }

        public async Task StartAsync()
        {
            Task startingStorage = _state.WithExclusiveAccess(state =>
            {
                Assert.State.Assert(!state.Running);
                state.Running = true;

                startingStorage = state.Configuration.IsPureClientEndpoint
                                       ? Task.CompletedTask
                                       : _storage.StartAsync();

                state.Poller = new NetMQPoller();
                state.Poller.RunAsync($"{nameof(Outbox)}_{nameof(state.Poller)}");
                return startingStorage;
            });

            await startingStorage.NoMarshalling();
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

        public void PublishTransactionally(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent)
        {
            var connections = _state.WithExclusiveAccess(state => state.Router.SubscriberConnectionsFor(exactlyOnceEvent)
                                                                       .Where(connection => connection.EndpointInformation.Id != state.Configuration.Id)
                                                                       .ToArray()); //We dispatch events to ourselves synchronously so don't go doing it again here.;

            //Urgent: bug. Our traceability thinking does not allow just discarding this message.But removing this if statement breaks a lot of tests that uses endpoint wiring but do not start an endpoint.
            if(connections.Length != 0) //Don't waste time if there are no receivers
            {
                var eventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
                _storage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);
                //Urgent: We should track a Task result here and record the message as being received on success and handle failure.
                Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection =>
                {
                    subscriberConnection.SendAsync(exactlyOnceEvent)
                                        .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id , exactlyOnceEvent.MessageId));
                }));
            }
        }

        public void SendTransactionally(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand)
        {
            var connection = _state.WithExclusiveAccess(state =>state.Router.ConnectionToHandlerFor(exactlyOnceCommand));

            _storage.SaveMessage(exactlyOnceCommand, connection.EndpointInformation.Id);

            Transaction.Current.OnCommittedSuccessfully(() => connection.SendAsync(exactlyOnceCommand)
                                                                        .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id , exactlyOnceCommand.MessageId)));
        }

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

        void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, Guid messageId)
        {
            if(completedSendTask.IsFaulted)
            {
                //Todo: Handle delivery failures sanely.
            } else
            {
                _storage.MarkAsReceived(messageId, receiverId);
            }
        }
    }
}
