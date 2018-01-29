using System;
using Composable.System.Threading;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            partial class Coordinator
            {
                // ReSharper disable once MemberCanBePrivate.Local Resharper bug....
                internal class QueuedMessage
                {
                    internal readonly TransportMessage.InComing TransportMessage;
                    readonly Coordinator _coordinator;
                    readonly Action _messageTask;
                    readonly ITaskRunner _taskRunner;
                    // ReSharper disable once UnusedMember.Local
                    public BusApi.IMessage DeserializeMessageAndCache() => TransportMessage.DeserializeMessageAndCacheForNextCall();
                    public Guid MessageId { get; }

                    public void Run()
                    {
                        _taskRunner.RunAndCrashProcessIfTaskThrows(() =>
                        {
                            try
                            {
                                _messageTask();
                                _coordinator.Succeeded(this);
                            }
                            catch(Exception exception)
                            {
                                _coordinator.Failed(this, exception);
                            }
                        });
                    }

                    public QueuedMessage(TransportMessage.InComing transportMessage, Coordinator coordinator, Action messageTask, ITaskRunner taskRunner)
                    {
                        MessageId = transportMessage.MessageId;
                        TransportMessage = transportMessage;
                        _coordinator = coordinator;
                        _messageTask = messageTask;
                        _taskRunner = taskRunner;
                    }
                }
            }
        }
    }
}
