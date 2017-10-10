using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class Implementation
        {
            // ReSharper disable InconsistentNaming
            public IGlobalBusStrateTracker _globalBusStrateTracker;
            public readonly Dictionary<Type, HashSet<DealerSocket>> _eventRoutes = new Dictionary<Type, HashSet<DealerSocket>>();
            public readonly Dictionary<Type, DealerSocket> _commandRoutes = new Dictionary<Type, DealerSocket>();
            public readonly Dictionary<Type, DealerSocket> _queryRoutes = new Dictionary<Type, DealerSocket>();

            public bool _running;
            public readonly IList<DealerSocket> _dealerSockets = new List<DealerSocket>();
            public readonly Dictionary<Guid, TaskCompletionSource<IMessage>> _outStandingTasks = new Dictionary<Guid, TaskCompletionSource<IMessage>>();
            public NetMQPoller _poller;
            // ReSharper restore InconsistentNaming
        }

        readonly IGuardedResource<Implementation> _this = GuardedResource<Implementation>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStrateTracker globalBusStrateTracker) => _this.Locked(@this => @this._globalBusStrateTracker = globalBusStrateTracker);

        public void Connect(IEndpoint endpoint) => _this.Locked(@this =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();

            var dealerSocket = new DealerSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            dealerSocket.Options.SendHighWatermark = int.MaxValue;
            dealerSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            dealerSocket.Options.Linger = 0.Milliseconds();

            dealerSocket.ReceiveReady += ReceiveResponse;
            @this._dealerSockets.Add(dealerSocket);
            dealerSocket.Connect(endpoint.Address);
            @this._poller.Add(dealerSocket);

            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    @this._eventRoutes.GetOrAdd(messageType, () => new HashSet<DealerSocket>()).Add(dealerSocket);
                } else if(IsCommand(messageType))
                {
                    @this._commandRoutes.Add(messageType, dealerSocket);
                } else if(IsQuery(messageType))
                {
                    @this._queryRoutes.Add(messageType, dealerSocket);
                }
            }
        });

        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var (message, task) = _this.Locked(@this =>
            {
                var theMessage = TransportMessage.ReadResponse((DealerSocket)e.Socket);
                var theTask = @this._outStandingTasks.GetAndRemove(theMessage.MessageId);

                return (theMessage, theTask);
            });

            if(message.SuccessFull)
            {
                task.SetResult(message.Result);
            } else
            {
                task.SetException(new Exception("Dispatching message failed"));
            }
        }

        static bool IsCommand(Type type) => typeof(ICommand).IsAssignableFrom(type);
        static bool IsEvent(Type type) => typeof(IEvent).IsAssignableFrom(type);
        static bool IsQuery(Type type) => typeof(IQuery).IsAssignableFrom(type);

        public void Stop()
        {
            var cheatingToAvoiddeadlockingWithReceiveResponseMethod = _this.Locked(@this =>
            {
                Contract.State.Assert(@this._running);
                @this._running = false;
                return @this;
            });

            cheatingToAvoiddeadlockingWithReceiveResponseMethod._poller.Dispose();

            _this.Locked(@this => @this._dealerSockets.ForEach(socket => socket.Dispose()));
        }

        public void Start() => _this.Locked(@this =>
        {
            Contract.State.Assert(!@this._running);
            @this._running = true;
            @this._poller = new NetMQPoller();
            @this._poller.RunAsync();
        });

        public void Dispatch(IEvent @event) => _this.Locked(@this =>
        {
            foreach(var socket in @this._eventRoutes[@event.GetType()])
            {
                @this._globalBusStrateTracker.SendingMessageOnTransport(@event);
                TransportMessage.Send(socket, @event);
            }
        });

        public void Dispatch(ICommand command) => _this.Locked(@this =>
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            @this._outStandingTasks.Add(command.MessageId, taskCompletionSource);
            @this._globalBusStrateTracker.SendingMessageOnTransport(command);
            TransportMessage.Send(@this._commandRoutes[command.GetType()], command);
        });

        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _this.Locked(@this =>
            {
                @this._outStandingTasks.Add(command.MessageId, taskCompletionSource);
                @this._globalBusStrateTracker.SendingMessageOnTransport(command);
                TransportMessage.Send(@this._commandRoutes[command.GetType()], command);
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
                TransportMessage.Send(@this._queryRoutes[query.GetType()], query);
            });
            return (TQueryResult)await taskCompletionSource.Task;
        }

        public void Dispose() => _this.Locked(@this =>
        {
            if(@this._running)
            {
                Stop();
            }
        });
    }
}
