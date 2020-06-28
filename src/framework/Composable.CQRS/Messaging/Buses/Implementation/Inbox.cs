﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox : IInbox, IDisposable
    {
        class Runner
        {
            readonly NetMQQueue<NetMQMessage> _responseQueue;
            readonly RouterSocket _serverSocket;
            readonly NetMQPoller _poller;
            readonly Thread _messageReceiverThread;
            readonly Thread _pollerThread;
            readonly CancellationTokenSource _cancellationTokenSource;
            readonly BlockingCollection<IReadOnlyList<TransportMessage.InComing>> _receivedMessageBatches = new BlockingCollection<IReadOnlyList<TransportMessage.InComing>>();
            readonly HandlerExecutionEngine _handlerExecutionEngine;
            readonly IMessageStorage _storage;
            readonly ITypeMapper _typeMapper;
            readonly IRemotableMessageSerializer _serializer;
            internal readonly EndPointAddress Address;

            public Runner(HandlerExecutionEngine handlerExecutionEngine, IMessageStorage storage, string address, RealEndpointConfiguration configuration, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
            {
                _handlerExecutionEngine = handlerExecutionEngine;
                _storage = storage;

                _serverSocket = new RouterSocket();
                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                _serverSocket.Options.SendHighWatermark = int.MaxValue;
                _serverSocket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                _serverSocket.Options.Linger = 0.Milliseconds();

                Address = new EndPointAddress(_serverSocket.BindAndReturnActualAddress(address));
                _serverSocket.ReceiveReady += HandleIncomingMessage;

                _responseQueue = new NetMQQueue<NetMQMessage>();

                _responseQueue.ReceiveReady += SendResponseMessage;

                _cancellationTokenSource = new CancellationTokenSource();
                _poller = new NetMQPoller {_serverSocket, _responseQueue};
                _pollerThread = new Thread(() => _poller.Run()) {Name = $"{nameof(Inbox)}_PollerThread_{configuration.Name}"};
                _pollerThread.Start();

                _messageReceiverThread = new Thread(MessageReceiverThread) {Name = $"{nameof(Inbox)}_{nameof(MessageReceiverThread)}_{configuration.Name}"};
                _messageReceiverThread.Start();

                _handlerExecutionEngine.Start();

                _typeMapper = typeMapper;
                _serializer = serializer;
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
                            //performance: With the current design having this code here causes all queries to wait for persisting of all transactional messages that arrived before them.
                            if(transportMessage.Is<MessageTypes.Remotable.IAtMostOnceMessage>())
                            {
                                //todo: handle the exception that will be thrown if this is a duplicate message
                                _storage.SaveIncomingMessage(transportMessage);

                                if(transportMessage.Is<MessageTypes.Remotable.ExactlyOnce.IMessage>())
                                {
                                    var persistedResponse = transportMessage.CreatePersistedResponse();
                                    _responseQueue.Enqueue(persistedResponse);
                                }
                            }

                            var dispatchTask = _handlerExecutionEngine.Enqueue(transportMessage);

                            dispatchTask.ContinueWith(dispatchResult =>
                            {
                                //refactor: Consider moving these responsibilities into the message class or other class. Probably create more subtypes so that no type checking is required. See also: HandlerExecutionEngine.Coordinator
                                var message = transportMessage.DeserializeMessageAndCacheForNextCall();
                                if(message is MessageTypes.Remotable.IRequireRemoteResponse)
                                {
                                    if(dispatchResult.IsFaulted)
                                    {
                                        var failureResponse = transportMessage.CreateFailureResponse(dispatchResult.Exception);
                                        _responseQueue.Enqueue(failureResponse);
                                    } else
                                    {
                                        Assert.Result.Assert(dispatchResult.IsCompleted);
                                        try
                                        {
                                            if(message is MessageTypes.IHasReturnValue)
                                            {
                                                var successResponse = transportMessage.CreateSuccessResponseWithData(dispatchResult.Result);
                                                _responseQueue.Enqueue(successResponse);
                                            } else
                                            {
                                                var successResponse = transportMessage.CreateSuccessResponse();
                                                _responseQueue.Enqueue(successResponse);
                                            }
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
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException || exception is ThreadAbortException) {}
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
                _cancellationTokenSource.Cancel();
                _messageReceiverThread.InterruptAndJoin();
                _poller.StopAsync();
                _pollerThread.Join();
                _poller.Dispose();
                _serverSocket.Close();
                _serverSocket.Dispose();
                _handlerExecutionEngine.Stop();

                _receivedMessageBatches.Dispose();
            }
        }

        Runner? _runner;
        readonly RealEndpointConfiguration _configuration;

        readonly string _address;
        readonly ITypeMapper _typeMapper;
        readonly IRemotableMessageSerializer _serializer;
        readonly IMessageStorage _storage;
        readonly HandlerExecutionEngine _handlerExecutionEngine;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, RealEndpointConfiguration configuration, IMessageStorage messageStorage, ITypeMapper typeMapper, ITaskRunner taskRunner, IRemotableMessageSerializer serializer)
        {
            _configuration = configuration;
            _typeMapper = typeMapper;
            _serializer = serializer;
            _address = configuration.Address;
            _storage = messageStorage;
            _handlerExecutionEngine = new HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, _storage, taskRunner);
        }

        public EndPointAddress Address => _runner?.Address ?? new EndPointAddress(_address);

        public async Task StartAsync()
        {
            Assert.State.Assert(_runner is null);
            var storageStartTask = _storage.StartAsync();
            _runner = new Runner(_handlerExecutionEngine, _storage, _address, _configuration, _typeMapper, _serializer);
            await storageStartTask;
        }

        public void Stop()
        {
            Assert.State.Assert(!(_runner is null));
            _runner.Dispose();
            _runner = null;
        }


        public void Dispose()
        {
            if(!(_runner is null))
            {
                Stop();
            }
        }


    }
}