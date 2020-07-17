using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.NetMQCE;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Threading.Tasks;
using Composable.SystemExtensions.TransactionsCE;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        class InboxConnection : IInboxConnection
        {
            internal MessageTypes.Internal.EndpointInformation EndpointInformation { get; private set; }
            readonly ITypeMapper _typeMapper;
            readonly ITaskRunner _taskRunner;
            readonly IRemotableMessageSerializer _serializer;

            public void DispatchIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.IEvent @event) => Transaction.Current.OnCommittedSuccessfully(
                () =>
                {
                    var message = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);
                    _state.WithExclusiveAccess(state => DispatchMessage(state, message));
                });

            public void DispatchIfTransactionCommits(MessageTypes.Remotable.ExactlyOnce.ICommand command) => Transaction.Current.OnCommittedSuccessfully(
                () =>
                {
                    var message = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);
                    _state.WithExclusiveAccess(state => DispatchMessage(state, message));
                });

            public async Task<TCommandResult> DispatchAsync<TCommandResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TCommandResult> command)
            {
                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    DispatchMessage(state, outGoingMessage);
                });

                return (TCommandResult)await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task DispatchAsync(MessageTypes.Remotable.AtMostOnce.ICommand command)
            {
                var taskCompletionSource = new VoidTaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    DispatchMessage(state, outGoingMessage);
                });

                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task<TQueryResult> DispatchAsync<TQueryResult>(MessageTypes.Remotable.NonTransactional.IQuery<TQueryResult> query)
            {
                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                var outGoingMessage = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);

                _state.WithExclusiveAccess(state =>
                {
                    state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                    state.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
                    state.DispatchQueue.Enqueue(outGoingMessage);
                });

                return (TQueryResult)await taskCompletionSource.Task.NoMarshalling();
            }

            static void DispatchMessage(InboxConnectionState @this, TransportMessage.OutGoing outGoingMessage)
            {
                if(outGoingMessage.IsExactlyOnceDeliveryMessage)
                {
                    @this.PendingDeliveryNotifications.Add(outGoingMessage.MessageId, @this.TimeSource.UtcNow);
                }

                @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
                @this.DispatchQueue.Enqueue(outGoingMessage);
            }

            internal async Task Init() { EndpointInformation = await DispatchAsync(new MessageTypes.Internal.EndpointInformationQuery()).NoMarshalling(); }

#pragma warning disable 8618 //Refactor: This really should not be suppressed. We do have a bad design that might cause null reference exceptions here if Init has not been called.
            internal InboxConnection(IGlobalBusStateTracker globalBusStateTracker,
#pragma warning restore 8618
                                     EndPointAddress serverEndpoint,
                                     NetMQPoller poller,
                                     IUtcTimeTimeSource timeSource,
                                     Outbox.IMessageStorage messageStorage,
                                     ITypeMapper typeMapper,
                                     ITaskRunner taskRunner,
                                     IRemotableMessageSerializer serializer)
            {
                _serializer = serializer;
                _typeMapper = typeMapper;
                _taskRunner = taskRunner;
                _state = new OptimizedThreadShared<InboxConnectionState>(new InboxConnectionState(timeSource, messageStorage, new DealerSocket(), globalBusStateTracker));

                _state.WithExclusiveAccess(state =>
                {
                    poller.Add(state.DispatchQueue);

                    state.DispatchQueue.ReceiveReady += DispatchQueuedMessages;

                    //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                    state.Socket.Options.SendHighWatermark = int.MaxValue;
                    state.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                    //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                    state.Socket.Options.Linger = 0.Milliseconds();

                    state.Socket.ReceiveReady += ReceiveResponse;

                    state.Socket.Connect(serverEndpoint);
                    poller.Add(state.Socket);
                });
            }

            void DispatchQueuedMessages(object sender, NetMQQueueEventArgs<TransportMessage.OutGoing> netMQQueueEventArgs) => _state.WithExclusiveAccess(state =>
            {
                while(netMQQueueEventArgs.Queue.TryDequeue(out var message, TimeSpan.Zero)) state.Socket.Send(message);
            });

            public void Dispose() => _state.WithExclusiveAccess(state => state.Dispose());

            class InboxConnectionState : IDisposable
            {
                internal readonly IGlobalBusStateTracker GlobalBusStateTracker;
                internal readonly Dictionary<Guid, TaskCompletionSource<object>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<object>>();
                internal readonly Dictionary<Guid, VoidTaskCompletionSource> ExpectedCompletionTasks = new Dictionary<Guid, VoidTaskCompletionSource>();
                internal readonly Dictionary<Guid, DateTime> PendingDeliveryNotifications = new Dictionary<Guid, DateTime>();
                internal readonly DealerSocket Socket;
                internal readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();
                internal IUtcTimeTimeSource TimeSource { get; private set; }
                internal Outbox.IMessageStorage Storage { get; private set; }

                public InboxConnectionState(IUtcTimeTimeSource timeSource, Outbox.IMessageStorage storage, DealerSocket socket, IGlobalBusStateTracker globalBusStateTracker)
                {
                    Socket = socket;
                    TimeSource = timeSource;
                    Storage = storage;
                    GlobalBusStateTracker = globalBusStateTracker;
                }

                public void Dispose()
                {
                    Socket.Dispose();
                    DispatchQueue.Dispose();
                }
            }

            readonly IThreadShared<InboxConnectionState> _state;

            //Runs on poller thread so NO BLOCKING HERE!
            void ReceiveResponse(object sender, NetMQSocketEventArgs e)
            {
                var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, _typeMapper, 100);

                _state.WithExclusiveAccess(state =>
                {
                    foreach(var response in responseBatch)
                    {
                        switch(response.ResponseType)
                        {
                            case TransportMessage.Response.ResponseType.SuccessWithData:
                            {
                                var successResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                                {
                                    try
                                    {
                                        successResponse.SetResult(Assert.Result.NotNull(response.DeserializeResult(_serializer))); //Refactor: Lying about nullability to the compiler is not pretty at all.
                                    }
                                    catch(Exception exception)
                                    {
                                        successResponse.SetException(exception);
                                    }
                                });
                            }
                                break;
                            case TransportMessage.Response.ResponseType.Success:
                            {
                                var successResponse = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                                {
                                    try
                                    {
                                        successResponse.SetResult();
                                    }
                                    catch(Exception exception)
                                    {
                                        successResponse.SetException(exception);
                                    }
                                });
                            }
                                break;
                            case TransportMessage.Response.ResponseType.FailureExpectedReturnValue:
                                var failureResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponse.SetException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
                                break;
                            case TransportMessage.Response.ResponseType.Failure:
                                var failureResponse2 = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponse2.SetException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
                                break;
                            case TransportMessage.Response.ResponseType.Received:
                                Assert.Result.Assert(state.PendingDeliveryNotifications.Remove(response.RespondingToMessageId));
                                _taskRunner.RunAndCrashProcessIfTaskThrows(() => state.Storage.MarkAsReceived(response, EndpointInformation.Id));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                });
            }
        }
    }
}