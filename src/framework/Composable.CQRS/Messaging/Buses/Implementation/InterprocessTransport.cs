using System;
using System.Collections.Generic;
using System.Threading;
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
        readonly IGlobalBusStrateTracker _globalBusStrateTracker;
        readonly Dictionary<Type, HashSet<DealerSocket>> _netMqEventRoutes = new Dictionary<Type, HashSet<DealerSocket>>();
        readonly Dictionary<Type, DealerSocket> _netMqCommandRoutes = new Dictionary<Type, DealerSocket>();
        readonly Dictionary<Type, DealerSocket> _netMqQueryRoutes = new Dictionary<Type, DealerSocket>();

        bool _running;
        readonly CancellationTokenSource _canceller = new CancellationTokenSource();
        IGuardedResource _guard = GuardedResource.WithTimeout(3.Seconds());
        readonly IList<DealerSocket> _dealerSockets = new List<DealerSocket>();
        readonly Dictionary<Guid, TaskCompletionSource<IMessage>> _outStandingTasks = new Dictionary<Guid, TaskCompletionSource<IMessage>>();

        NetMQPoller _poller;

        public InterprocessTransport(IGlobalBusStrateTracker globalBusStrateTracker) => _globalBusStrateTracker = globalBusStrateTracker;

        public void Connect(IEndpoint endpoint) => _guard.Update(() =>
        {
            var messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();
            var inbox = endpoint.ServiceLocator.Resolve<IInbox>();

            var dealerSocket = new DealerSocket();
            dealerSocket.ReceiveReady += ReceiveResponse;
            _dealerSockets.Add(dealerSocket);
            dealerSocket.Options.Linger = 0.Milliseconds();
            dealerSocket.Connect(inbox.Address);
            _poller.Add(dealerSocket);

            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    Contract.Argument.Assert(!IsCommand(messageType), !IsQuery(messageType));
                    _netMqEventRoutes.GetOrAdd(messageType, () => new HashSet<DealerSocket>()).Add(dealerSocket);
                } else if(typeof(ICommand).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsQuery(messageType), !_netMqCommandRoutes.ContainsKey(messageType));
                    _netMqCommandRoutes.Add(messageType, dealerSocket);
                } else if(typeof(IQuery).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsCommand(messageType), !_netMqQueryRoutes.ContainsKey(messageType));
                    _netMqQueryRoutes.Add(messageType, dealerSocket);
                }
            }
        });

        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var message = _guard.Update(() => TransportMessage.ReadResponse((DealerSocket)e.Socket));

            var task = _outStandingTasks[message.MessageId];
            _outStandingTasks.Remove(message.MessageId);

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
            Contract.State.Assert(_running);
            _running = false;
            _canceller.Cancel();
            _poller.Dispose();
            _dealerSockets.ForEach(@this => @this.Dispose());
        }

        public void Start() => _guard.Update(() =>
        {
            Contract.State.Assert(!_running);
            _running = true;
            _poller = new NetMQPoller();
            _poller.RunAsync();
        });

        public void Dispatch(IEvent @event) => _guard.Update(() =>
        {
            foreach(var socket in _netMqEventRoutes[@event.GetType()])
            {
                _globalBusStrateTracker.SendingMessageOnTransport(@event);
                TransportMessage.Send(socket, @event);
            }
        });

        public void Dispatch(ICommand command) => _guard.Update(() =>
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outStandingTasks.Add(command.MessageId, taskCompletionSource);
            _globalBusStrateTracker.SendingMessageOnTransport(command);
            TransportMessage.Send(_netMqCommandRoutes[command.GetType()], command);
        });

        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _guard.Update(() =>
            {
                _outStandingTasks.Add(command.MessageId, taskCompletionSource);
                _globalBusStrateTracker.SendingMessageOnTransport(command);
                TransportMessage.Send(_netMqCommandRoutes[command.GetType()], command);
            });
            return (TCommandResult)await taskCompletionSource.Task;
        }

        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _guard.Update(() =>
            {
                _outStandingTasks.Add(query.MessageId, taskCompletionSource);
                _globalBusStrateTracker.SendingMessageOnTransport(query);
                TransportMessage.Send(_netMqQueryRoutes[query.GetType()], query);
            });
            return (TQueryResult)await taskCompletionSource.Task;
        }

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
        }
    }
}
