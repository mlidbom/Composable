using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Messaging.NetMQCE;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox
    {
        internal class InboxConnection : IInboxConnection
        {
            readonly IThreadShared<InboxConnectionState> _state;
            public MessageTypes.Internal.EndpointInformation EndpointInformation { get; private set; }
            readonly ITypeMapper _typeMapper;
            readonly IRemotableMessageSerializer _serializer;
            readonly IGlobalBusStateTracker _globalBusStateTracker;
            readonly NetMQQueue<TransportMessage.OutGoing> _sendQueue = new NetMQQueue<TransportMessage.OutGoing>();
            // ReSharper disable once InconsistentNaming we use this naming variation to try and make it extra clear that this must only ever be accessed from the poller thread.
            readonly IDisposable _socketDisposable;

            public async Task SendAsync(IExactlyOnceEvent @event)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(@event, _typeMapper, _serializer);

                _state.Update(state => state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource));
                SendMessage(outGoingMessage);
                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task SendAsync(IExactlyOnceCommand command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.Update(state => state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource));
                SendMessage(outGoingMessage);
                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task<TCommandResult> PostAsync<TCommandResult>(IAtMostOnceCommand<TCommandResult> command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource<Func<object>>();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.Update(state => state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource));
                SendMessage(outGoingMessage);
                return (TCommandResult)(await taskCompletionSource.Task.NoMarshalling()).Invoke();
            }

            public async Task PostAsync(IAtMostOnceHypermediaCommand command)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource();
                var outGoingMessage = TransportMessage.OutGoing.Create(command, _typeMapper, _serializer);

                _state.Update(state => state.ExpectedCompletionTasks.Add(outGoingMessage.MessageId, taskCompletionSource));
                SendMessage(outGoingMessage);
                await taskCompletionSource.Task.NoMarshalling();
            }

            public async Task<TQueryResult> GetAsync<TQueryResult>(IRemotableQuery<TQueryResult> query)
            {
                var taskCompletionSource = new AsyncTaskCompletionSource<Func<object>>();
                var outGoingMessage = TransportMessage.OutGoing.Create(query, _typeMapper, _serializer);

                _state.Update(state => state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource));
                SendMessage(outGoingMessage);
                return (TQueryResult)(await taskCompletionSource.Task.NoMarshalling()).Invoke();
            }

            void SendMessage(TransportMessage.OutGoing outGoingMessage)
            {
                _globalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
                _sendQueue.Enqueue(outGoingMessage);
            }

            internal async Task Init() { EndpointInformation = await GetAsync(new MessageTypes.Internal.EndpointInformationQuery()).NoMarshalling(); }

#pragma warning disable 8618 //Refactor: This really should not be suppressed. We do have a bad design that might cause null reference exceptions here if Init has not been called.
            internal InboxConnection(IGlobalBusStateTracker globalBusStateTracker,
#pragma warning restore 8618
                                     EndPointAddress serverEndpoint,
                                     NetMQPoller poller,
                                     ITypeMapper typeMapper,
                                     IRemotableMessageSerializer serializer)
            {
                _serializer = serializer;
                _typeMapper = typeMapper;
                _globalBusStateTracker = globalBusStateTracker;
                var socket = new DealerSocket();
                _socketDisposable = socket;//Getting rid of the type means we don't need to worry about usage from the wrong threads.
                _state = ThreadShared.WithDefaultTimeout(new InboxConnectionState());

                poller.Add(_sendQueue);

                SendMessagesReceivedOnQueueThroughSocket_PollerThread(socket);

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                socket.Options.SendHighWatermark = int.MaxValue;
                socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                socket.Options.Linger = 0.Milliseconds();

                socket.ReceiveReady += ReceiveResponse_PollerThread;

                socket.Connect(serverEndpoint);
                poller.Add(socket);
            }

            void SendMessagesReceivedOnQueueThroughSocket_PollerThread(DealerSocket socket)
            {
                _sendQueue.ReceiveReady += (sender, netMQQueueEventArgs) =>
                {
                    while(netMQQueueEventArgs.Queue.TryDequeue(out var message, TimeSpan.Zero)) socket.Send(message);
                };
            }

            public void Dispose()
            {
                _sendQueue.Dispose();
                _socketDisposable.Dispose();
            }

            class InboxConnectionState
            {
                internal readonly Dictionary<Guid, AsyncTaskCompletionSource<Func<object>>> ExpectedResponseTasks = new Dictionary<Guid, AsyncTaskCompletionSource<Func<object>>>();
                internal readonly Dictionary<Guid, AsyncTaskCompletionSource> ExpectedCompletionTasks = new Dictionary<Guid, AsyncTaskCompletionSource>();
            }

            //Runs on poller thread so NO BLOCKING HERE!
            void ReceiveResponse_PollerThread(object? sender, NetMQSocketEventArgs e)
            {
                var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, _typeMapper, _serializer, batchMaximum: 100);

                _state.Update(state =>
                {
                    foreach(var response in responseBatch)
                    {
                        switch(response.ResponseType)
                        {
                            case TransportMessage.Response.ResponseType.Received:
                            case TransportMessage.Response.ResponseType.Success:
                                var successResponse = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                successResponse.ScheduleContinuation();
                                break;
                            case TransportMessage.Response.ResponseType.SuccessWithData:
                                var successResponseWithData = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                successResponseWithData.ScheduleContinuation(response.DeserializeResult);
                                break;
                            case TransportMessage.Response.ResponseType.Failure:
                                var failureResponse = state.ExpectedCompletionTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponse.ScheduleException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
                                break;
                            case TransportMessage.Response.ResponseType.FailureExpectedReturnValue:
                                var failureResponseExpectingData = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                                failureResponseExpectingData.ScheduleException(new MessageDispatchingFailedException(response.Body ?? "Got no exception text from remote end."));
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