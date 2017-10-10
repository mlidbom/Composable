using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.NewtonSoft;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

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
            var message = _guard.Update(() => e.Socket.ReceiveMultipartMessage());

            var messageId = new Guid(message[0].ToByteArray());
            var result = message[1].ConvertToString();
            var task = _outStandingTasks[messageId];
            _outStandingTasks.Remove(messageId);

            if(result == "OK")
            {

                var responseType = message[2].ConvertToString().AsType();
                var responseBody = message[3].ConvertToString();
                var responseObject = JsonConvert.DeserializeObject(responseBody, responseType, JsonSettings.JsonSerializerSettings);
                task.SetResult((IMessage)responseObject);
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
                socket.SendMoreFrame(@event.MessageId.ToByteArray());
                socket.SendMoreFrame(@event.GetType().FullName);
                socket.SendFrame(JsonConvert.SerializeObject(@event, Formatting.Indented, JsonSettings.JsonSerializerSettings));
            }
        });

        public void Dispatch(ICommand command) => _guard.Update(() =>
        {
                        var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outStandingTasks.Add(command.MessageId, taskCompletionSource);
            _guard.Update(() =>
            {
                _globalBusStrateTracker.SendingMessageOnTransport(command);
                var socket = _netMqCommandRoutes[command.GetType()];
                socket.SendMoreFrame(command.MessageId.ToByteArray());
                socket.SendMoreFrame(command.GetType().FullName);
                socket.SendFrame(JsonConvert.SerializeObject(command, Formatting.Indented, JsonSettings.JsonSerializerSettings));
            });
        });

        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outStandingTasks.Add(command.MessageId, taskCompletionSource);
            _guard.Update(() =>
            {
                _globalBusStrateTracker.SendingMessageOnTransport(command);
                var socket = _netMqCommandRoutes[command.GetType()];
                socket.SendMoreFrame(command.MessageId.ToByteArray());
                socket.SendMoreFrame(command.GetType().FullName);
                socket.SendFrame(JsonConvert.SerializeObject(command, Formatting.Indented, JsonSettings.JsonSerializerSettings));
            });

            return (TCommandResult)await taskCompletionSource.Task;
        }

        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult
        {
            var taskCompletionSource = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outStandingTasks.Add(query.MessageId,taskCompletionSource);
            _guard.Update(() =>
            {
                _globalBusStrateTracker.SendingMessageOnTransport(query);
                var socket = _netMqQueryRoutes[query.GetType()];
                socket.SendMoreFrame(query.MessageId.ToByteArray());
                socket.SendMoreFrame(query.GetType().FullName);
                socket.SendFrame(JsonConvert.SerializeObject(query, Formatting.Indented, JsonSettings.JsonSerializerSettings));
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

    class TransportMessage
    {
        public IMessage Message { get; }
        public byte[] Client { get; }

        TransportMessage(IMessage message, byte[] client)
        {
            Message = message;
            Client = client;
        }

        public static TransportMessage ReadFromSocket(RouterSocket socket)
        {
            var receivedMessage = socket.ReceiveMultipartMessage();

            var client = receivedMessage[0].ToByteArray();
            var messageId = new Guid(receivedMessage[1].ToByteArray());
            var messageTypeString = receivedMessage[2].ConvertToString();
            var messageBody = receivedMessage[3].ConvertToString();
            var messageType = messageTypeString.AsType();

            var message = (IMessage)JsonConvert.DeserializeObject(messageBody, messageType, JsonSettings.JsonSerializerSettings);

            Contract.State.Assert(messageId == message.MessageId);

            return new TransportMessage(message, client);
        }

        public void RespondSucess(IMessage response, RouterSocket socket)
        {
            var netMqMessage = new NetMQMessage();

            netMqMessage.Append(Client);
            netMqMessage.Append(Message.MessageId.ToByteArray());
            netMqMessage.Append("OK");

            netMqMessage.Append(response.GetType().FullName);
            netMqMessage.Append(JsonConvert.SerializeObject(response, Formatting.Indented, JsonSettings.JsonSerializerSettings));

            socket.SendMultipartMessage(netMqMessage);
        }

        public void RespondError(Exception exception, RouterSocket socket)
        {
            var netMqMessage = new NetMQMessage();

            netMqMessage.Append(Client);
            netMqMessage.Append(Message.MessageId.ToByteArray());
            netMqMessage.Append("FAIL");

            socket.SendMultipartMessage(netMqMessage);
        }

        enum Type
        {
            Event = 1,
            Query = 2, 
            Command = 3,
            CommandWithResult = 4
        }
    }
}
