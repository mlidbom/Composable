using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
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

        NetMQQueue<NetMQMessage> _responseQueue;

        RouterSocket _serverSocket;

        NetMQPoller _poller;
        readonly BlockingCollection<IReadOnlyList<TransportMessage.InComing>> _receivedMessageBatches = new BlockingCollection<IReadOnlyList<TransportMessage.InComing>>();
        readonly MessageStorage _storage;
        readonly HandlerExecutionEngine _handlerExecutionEngine;
        Thread _messageReceiverThread;
        Thread _pollerThread;
        CancellationTokenSource _cancellationTokenSource;
        IRemotableMessageSerializer _serializer;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, EndpointConfiguration configuration, ISqlConnectionProvider connectionFactory, ITypeMapper typeMapper, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
        {
            _configuration = configuration;
            _typeMapper = typeMapper;
            _serializer = serializer;
            _address = configuration.Address;
            _storage = new MessageStorage(connectionFactory);
            _handlerExecutionEngine = new HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, _storage, taskRunner);
        }

        public EndPointAddress Address => new EndPointAddress(_address);

        public async Task StartAsync() => await _resourceGuard.Update(async () =>
        {
            Assert.State.Assert(!_running);
            _running = true;

            var storageStartTask = _storage.StartAsync();

            _serverSocket = new RouterSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            _serverSocket.Options.SendHighWatermark = int.MaxValue;
            _serverSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            _serverSocket.Options.Linger = 0.Milliseconds();

            _address = _serverSocket.BindAndReturnActualAddress(_address);
            _serverSocket.ReceiveReady += HandleIncomingMessage;

            _responseQueue = new NetMQQueue<NetMQMessage>();

            _responseQueue.ReceiveReady += SendResponseMessage;

            _cancellationTokenSource = new CancellationTokenSource();
            _poller = new NetMQPoller() {_serverSocket, _responseQueue};
            _pollerThread = new Thread(() => _poller.Run()){Name = $"{nameof(Inbox)}_{nameof(_pollerThread)}_{_configuration.Name}"};
            _pollerThread.Start();

            _messageReceiverThread = new Thread(MessageReceiverThread){Name = $"{nameof(Inbox)}_{nameof(MessageReceiverThread)}_{_configuration.Name}"};
            _messageReceiverThread.Start();

            _handlerExecutionEngine.Start();
            await storageStartTask;
        });

        public void Stop()
        {
            Assert.State.Assert(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _messageReceiverThread.InterruptAndJoin();
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
            try
            {
                while(true)
                {
                    var transportMessageBatch = _receivedMessageBatches.Take(_cancellationTokenSource.Token);
                    foreach(var transportMessage in transportMessageBatch)
                    {
                        if(transportMessage.Is<BusApi.Remotable.IAtMostOnceMessage>())
                        {
                            //todo: handle the exception that will be thrown if this is a duplicate message
                            _storage.SaveMessage(transportMessage);

                            if(transportMessage.Is<BusApi.Remotable.ExactlyOnce.IMessage>())
                            {
                                var persistedResponse = transportMessage.CreatePersistedResponse();
                                _responseQueue.Enqueue(persistedResponse);
                            }
                        }

                        var dispatchTask = _handlerExecutionEngine.Enqueue(transportMessage);

                        dispatchTask.ContinueWith(dispatchResult =>
                        {
                            var message = transportMessage.DeserializeMessageAndCacheForNextCall();
                            if(message is BusApi.Remotable.IRequireRemoteResponse)
                            {
                                if(dispatchResult.IsFaulted)
                                {
                                    var failureResponse = transportMessage.CreateFailureResponse(dispatchResult.Exception);
                                    _responseQueue.Enqueue(failureResponse);
                                } else if(dispatchResult.IsCompleted)
                                {
                                    try
                                    {
                                        var successResponse = transportMessage.CreateSuccessResponse(dispatchResult.Result);
                                        _responseQueue.Enqueue(successResponse);
                                    }
                                    catch(Exception exception)
                                    {
                                        var failureResponse = transportMessage.CreateFailureResponse(new AggregateException(exception));
                                        _responseQueue.Enqueue(failureResponse);
                                    }
                                }
                            }
                        });
                    }
                }
            }
            catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException)
            {
            }
        }

        void SendResponseMessage(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            while(e.Queue.TryDequeue(out var response, TimeSpan.Zero))
            {
                _serverSocket.SendMultipartMessage(response);
            }
        }

        void HandleIncomingMessage(object sender, NetMQSocketEventArgs e)
        {
            Assert.Argument.Assert(e.IsReadyToReceive);
            var batch = TransportMessage.InComing.ReceiveBatch(_serverSocket, _typeMapper, _serializer);
            _receivedMessageBatches.Add(batch);
        }

        public void Dispose()
        {
            if(_running)
                Stop();
        }
    }
}
