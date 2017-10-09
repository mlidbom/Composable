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
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses.Implementation
{
    class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        readonly Dictionary<Type, IList<IInbox>> _eventRoutes = new Dictionary<Type, IList<IInbox>>();
        readonly Dictionary<Type, IInbox> _commandRoutes = new Dictionary<Type, IInbox>();
        readonly Dictionary<Type, IInbox> _queryRoutes = new Dictionary<Type, IInbox>();
        readonly Dictionary<Type, List<RequestSocket>> _netMqRoutes = new Dictionary<Type, List<RequestSocket>>(); 

        bool _running;
        CancellationTokenSource _canceller = new CancellationTokenSource();
        Thread _sendThread;
        readonly ManualResetEvent _sendThreadStarted = new ManualResetEvent(false);
        readonly BlockingCollection<IMessage> _sendQueue = new BlockingCollection<IMessage>();

        public void Connect(IEndpoint endpoint)
        {
            IMessageHandlerRegistry messageHandlers = endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>();
            var inbox = endpoint.ServiceLocator.Resolve<IInbox>();
            foreach(var messageType in messageHandlers.HandledTypes())
            {
                if(IsEvent(messageType))
                {
                    Contract.Argument.Assert(!IsCommand(messageType), !IsQuery(messageType));
                    _eventRoutes.GetOrAdd(messageType, () => new List<IInbox>()).Add(inbox);
                } else if(typeof(ICommand).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsQuery(messageType), !_commandRoutes.ContainsKey(messageType));
                    _commandRoutes.Add(messageType, inbox);
                } else if(typeof(IQuery).IsAssignableFrom(messageType))
                {
                    Contract.Argument.Assert(!IsEvent(messageType), !IsCommand(messageType), !_queryRoutes.ContainsKey(messageType));
                    _queryRoutes.Add(messageType, inbox);
                }
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
        }

        public void Start()
        {
            Contract.State.Assert(!_running);
            _running = true;
            _sendThread = new Thread(SendThread)
                          {
                              Name = $"{nameof(InterprocessTransport)}_{nameof(SendThread)}"
                          };
            _sendThread.Start();
            Contract.Result.Assert(_sendThreadStarted.WaitOne(1.Seconds()));
        }

        void SendThread()
        {
            _sendThreadStarted.Set();

            while(!_canceller.IsCancellationRequested)
            {
               var message = _sendQueue.Take();

                if(!_netMqRoutes.TryGetValue(message.GetType(), out List<RequestSocket> requestSocket))
                {
                    switch(message)
                    {
                        case ICommand command:
                            requestSocket = new List<RequestSocket>() { new RequestSocket() };
                            requestSocket.Single().Connect(_commandRoutes[message.GetType()].Address);
                            break;
                        case IEvent @event:
                            requestSocket = _eventRoutes[@event.GetType()].Select(inbox => new RequestSocket(inbox.Address)).ToList();
                            break;
                        case IQuery query:
                            requestSocket = new List<RequestSocket>() { new RequestSocket() };
                            requestSocket.Single().Connect(_queryRoutes[message.GetType()].Address);
                            break;
                    }
                    _netMqRoutes.Add(message.GetType(), requestSocket);
                }

                foreach(var socket in requestSocket)
                {
                    socket.SendMoreFrame(message.GetType().FullName);
                    socket.SendFrame(JsonConvert.SerializeObject(message, Formatting.Indented, JsonSettings.JsonSerializerSettings));
                    var response = socket.ReceiveMultipartMessage();
                }
            }
        }

        public void Dispatch(IEvent @event)
        {
            _eventRoutes[@event.GetType()].ForEach(inbox => inbox.Dispatch(@event));
        }

        public void Dispatch(ICommand command)
        {
            _sendQueue.Add(command);
            _commandRoutes[command.GetType()].Dispatch(command);
        }
        public async Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage => (TCommandResult)await _commandRoutes[command.GetType()].Dispatch(command);
        public async Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> query) where TQueryResult : IQueryResult => (TQueryResult)await _queryRoutes[query.GetType()].Dispatch(query);



        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
        }
    }
}
