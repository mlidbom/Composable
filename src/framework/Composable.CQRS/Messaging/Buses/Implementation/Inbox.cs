using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox : IInbox, IDisposable
    {
        readonly EndpointConfiguration _configuration;
        readonly ITypeMapper _typeMapper;
        readonly IResourceGuard _resourceGuard = ResourceGuard.WithTimeout(1.Seconds());

        bool _running;
        string _address;

        NetMQQueue<TransportMessage.Response.Outgoing> _responseQueue;

        RouterSocket _serverSocket;

        NetMQPoller _poller;
        readonly BlockingCollection<IReadOnlyList<TransportMessage.InComing>> _receivedMessageBatches = new BlockingCollection<IReadOnlyList<TransportMessage.InComing>>();
        readonly MessageStorage _storage;
        readonly HandlerExecutionEngine _handlerExecutionEngine;
        Thread _messagePumpThread;
        Thread _pollerThread;
        CancellationTokenSource _cancellationTokenSource;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, EndpointConfiguration configuration, ISqlConnection connectionFactory, ITypeMapper typeMapper, ITaskRunner taskRunner)
        {
            _configuration = configuration;
            _typeMapper = typeMapper;
            _address = configuration.Address;
            _storage = new MessageStorage(connectionFactory);
            _handlerExecutionEngine = new HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, _storage, _typeMapper, taskRunner);
        }

        public EndPointAddress Address => new EndPointAddress(_address);

        public void Start() => _resourceGuard.Update(action: () =>
        {
            Assert.Invariant.Assert(!_running);
            _running = true;

            _serverSocket = new RouterSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            _serverSocket.Options.SendHighWatermark = int.MaxValue;
            _serverSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            _serverSocket.Options.Linger = 0.Milliseconds();

            _address = _serverSocket.BindAndReturnActualAddress(_address);
            _serverSocket.ReceiveReady += HandleIncomingMessage;

            _responseQueue = new NetMQQueue<TransportMessage.Response.Outgoing>();

            _responseQueue.ReceiveReady += SendResponseMessage;

            _cancellationTokenSource = new CancellationTokenSource();
            _poller = new NetMQPoller() {_serverSocket, _responseQueue};
            _pollerThread = new Thread(() => _poller.Run()){Name = $"{_configuration.Name}_{nameof(Inbox)}_{nameof(_pollerThread)}"};
            _pollerThread.Start();

            _messagePumpThread = new Thread(MessageReceiverThread){Name = $"{_configuration.Name}_{nameof(Inbox)}_{nameof(MessageReceiverThread)}"};
            _messagePumpThread.Start();

            _handlerExecutionEngine.Start();
            _storage.Start();
        });

        public void Stop()
        {
            Assert.Invariant.Assert(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _messagePumpThread.InterruptAndJoin();
            _poller.StopAsync();
            _pollerThread.Join();
            _poller.Dispose();
            _serverSocket.Close();
            _serverSocket.Dispose();
            _handlerExecutionEngine.Stop();
            _responseQueue = null;
        }

        void MessageReceiverThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var transportMessageBatch = _receivedMessageBatches.Take(_cancellationTokenSource.Token);
                    foreach(var transportMessage in transportMessageBatch)
                    {
                        var innerMessage = transportMessage.DeserializeMessageAndCacheForNextCall(_typeMapper);
                        if(innerMessage is BusApi.Remotable.ExactlyOnce.IMessage)
                        {
                            _storage.SaveMessage(transportMessage);
                            _responseQueue.Enqueue(transportMessage.CreatePersistedResponse());
                        }

                        var dispatchTask = _handlerExecutionEngine.Enqueue(transportMessage);

                        dispatchTask.ContinueWith(dispatchResult =>
                        {
                            var message = transportMessage.DeserializeMessageAndCacheForNextCall(_typeMapper);
                            if(message is BusApi.Remotable.IRequireRemoteResponse)
                            {
                                if(dispatchResult.IsFaulted)
                                {
                                    _responseQueue.Enqueue(transportMessage.CreateFailureResponse(dispatchResult.Exception));
                                } else if(dispatchResult.IsCompleted)
                                {
                                    _responseQueue.Enqueue(transportMessage.CreateSuccessResponse(dispatchResult.Result));
                                }
                            }
                        });
                    }
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
                {
                    return;
                }
            }
        }

        void SendResponseMessage(object sender, NetMQQueueEventArgs<TransportMessage.Response.Outgoing> e)
        {
            while(e.Queue.TryDequeue(out var response, TimeSpan.Zero))
            {
                _serverSocket.Send(response);
            }
        }

        void HandleIncomingMessage(object sender, NetMQSocketEventArgs e)
        {
            Assert.Argument.Assert(e.IsReadyToReceive);
            _receivedMessageBatches.Add(TransportMessage.InComing.ReceiveBatch(_serverSocket));
        }

        public void Dispose()
        {
            if(_running)
                Stop();
        }
    }
}
