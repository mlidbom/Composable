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
            // ReSharper disable InconsistentNaming
            public IGlobalBusStrateTracker _globalBusStrateTracker;
            public readonly Dictionary<Guid, TaskCompletionSource<IMessage>> _outStandingTasks = new Dictionary<Guid, TaskCompletionSource<IMessage>>();
            public DealerSocket _socket;
            public NetMQPoller _poller;
            public NetMQQueue<IMessage> _dispatchQueue = new NetMQQueue<IMessage>();
            // ReSharper restore InconsistentNaming

            public void DispatchMessage(object sender, NetMQQueueEventArgs<IMessage> e)
            {
                while (e.Queue.TryDequeue(out IMessage message, TimeSpan.Zero))
                {
                    TransportMessage.Send(_socket, message);
                }
            }
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public ClientConnection(IGlobalBusStrateTracker globalBusStrateTracker, IEndpoint endpoint, NetMQPoller poller)
        {
            _this.Locked(@this =>
            {
                @this._globalBusStrateTracker = globalBusStrateTracker;

                @this._poller = poller;

                @this._poller.Add(@this._dispatchQueue);

                @this._dispatchQueue.ReceiveReady += @this.DispatchMessage;

                @this._socket = new DealerSocket();

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                @this._socket.Options.SendHighWatermark = int.MaxValue;
                @this._socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                @this._socket.Options.Linger = 0.Milliseconds();

                @this._socket.ReceiveReady += ReceiveResponse;

                @this._socket.Connect(endpoint.Address);
                poller.Add(@this._socket);
            });
        }

        //Runs on poller thread so NO BLOCKING HERE!
        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            //todo: performance: The message is deserialized here which adds some time to execution. Move this to another thread to avoid blocking poller thread.
            var message = TransportMessage.ReadResponse(e.Socket);

            var completedTask = _this.Locked(@this => @this._outStandingTasks.GetAndRemove(message.MessageId));

            if (message.SuccessFull)
            {
                Task.Run(() =>
                {
                    try
                    {
                        completedTask.SetResult(message.DeserializeResult());
                    }
                    catch(Exception exception)
                    {
                        completedTask.SetException(exception);
                    }
                });
            } else
            {
                completedTask.SetException(new Exception("Dispatching message failed"));
            }
        }

        public void Dispatch(IEvent @event) => _this.Locked(@this =>
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            @this._outStandingTasks.Add(@event.MessageId, taskCompletionSource);
            @this._globalBusStrateTracker.SendingMessageOnTransport(@event);

            @this._dispatchQueue.Enqueue(@event);
        });

        public void Dispatch(ICommand command) => _this.Locked(@this =>
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            @this._outStandingTasks.Add(command.MessageId, taskCompletionSource);
            @this._globalBusStrateTracker.SendingMessageOnTransport(command);

            @this._dispatchQueue.Enqueue(command);
        });

        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _this.Locked(@this =>
            {
                @this._outStandingTasks.Add(command.MessageId, taskCompletionSource);
                @this._globalBusStrateTracker.SendingMessageOnTransport(command);
                @this._dispatchQueue.Enqueue(command);
            });
            return (TCommandResult)await taskCompletionSource.Task;
        }

        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _this.Locked(@this =>
            {
                @this._outStandingTasks.Add(query.MessageId, taskCompletionSource);
                @this._globalBusStrateTracker.SendingMessageOnTransport(query);
                @this._dispatchQueue.Enqueue(query);
            });
            return (TQueryResult)await taskCompletionSource.Task;
        }

        public void Dispose() => _this.Locked(@this =>
        {
            @this._poller.Remove(@this._dispatchQueue);
            @this._poller.Remove(@this._socket);
            @this._socket.Dispose();
        });
    }
}
