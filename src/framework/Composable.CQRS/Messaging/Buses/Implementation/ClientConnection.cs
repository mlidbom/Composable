using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class ClientConnection
    {
        class Implementation
        {
            public IGlobalBusStrateTracker GlobalBusStrateTracker;
            public readonly Dictionary<Guid, TaskCompletionSource<IMessage>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<IMessage>>();
            public DealerSocket Socket;
            public NetMQPoller Poller;
            public readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();

            public void DispatchMessage(object sender, NetMQQueueEventArgs<TransportMessage.OutGoing> e)
            {
                while(e.Queue.TryDequeue(out var message, TimeSpan.Zero))
                {
                    Socket.Send(message);
                }
            }
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public ClientConnection(IGlobalBusStrateTracker globalBusStrateTracker, IEndpoint endpoint, NetMQPoller poller)
        {
            _this.Locked(@this =>
            {
                @this.GlobalBusStrateTracker = globalBusStrateTracker;

                @this.Poller = poller;

                @this.Poller.Add(@this.DispatchQueue);

                @this.DispatchQueue.ReceiveReady += @this.DispatchMessage;

                @this.Socket = new DealerSocket();

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                @this.Socket.Options.SendHighWatermark = int.MaxValue;
                @this.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                @this.Socket.Options.Linger = 0.Milliseconds();

                @this.Socket.ReceiveReady += ReceiveResponse;

                @this.Socket.Connect(endpoint.Address);
                poller.Add(@this.Socket);
            });
        }

        //Runs on poller thread so NO BLOCKING HERE!
        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, batchMaximum: 100);

            _this.Locked(@this =>
            {
                foreach(var response in responseBatch)
                {
                    var responseTask = @this.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                    if(response.SuccessFull)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                responseTask.SetResult(response.DeserializeResult());
                            }
                            catch(Exception exception)
                            {
                                responseTask.SetException(exception);
                            }
                        });
                    } else
                    {
                        responseTask.SetException(new Exception("Dispatching message failed"));
                    }
                }
            });
        }

        public void Dispatch(IEvent @event) => DispatchMessage(@event);

        public void Dispatch(IDomainCommand command) => DispatchMessage(command);

        public async Task<TCommandResult> Dispatch<TCommandResult>(IDomainCommand<TCommandResult> command)
            => (TCommandResult)await DispatchMessage(command);

        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query)
            => (TQueryResult)await DispatchMessage(query);

        Task<IMessage> DispatchMessage(IMessage message) => _this.Locked(@this =>
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(message);

            if (message.RequiresResponse())
            {
                @this.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
            } else
            {
                taskCompletionSource.SetResult(null);
            }

            @this.GlobalBusStrateTracker.SendingMessageOnTransport(outGoingMessage);
            @this.DispatchQueue.Enqueue(outGoingMessage);

            return taskCompletionSource.Task;
        });


        public void Dispose() => _this.Locked(@this =>
        {
            @this.Socket.Dispose();
            @this.DispatchQueue.Dispose();
        });
    }
}
