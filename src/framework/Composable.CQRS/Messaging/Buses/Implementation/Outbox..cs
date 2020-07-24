using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.TransactionsCE;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Outbox : IOutbox
    {
        readonly Outbox.IMessageStorage _storage;
        readonly RealEndpointConfiguration _configuration;
        readonly ITransport _transport;

        public Outbox(ITransport transport, Outbox.IMessageStorage messageStorage, RealEndpointConfiguration configuration)
        {
            _storage = messageStorage;
            _transport = transport;
            _configuration = configuration;
        }

        public void PublishTransactionally(MessageTypes.Remotable.ExactlyOnce.IEvent exactlyOnceEvent)
        {
            var connections  = _transport.SubscriberConnectionsFor(exactlyOnceEvent)
                                  .Where(connection => connection.EndpointInformation.Id != _configuration.Id)
                                  .ToArray(); //We dispatch events to ourselves synchronously so don't go doing it again here.;

            //Urgent: bug. Our traceability thinking does not allow just discarding this message.But removing this if statement breaks a lot of tests that uses endpoint wiring but do not start an endpoint.
            if(connections.Length != 0)
            {
                var eventHandlerEndpointIds = connections.Select(connection => connection.EndpointInformation.Id).ToArray();
                _storage.SaveMessage(exactlyOnceEvent, eventHandlerEndpointIds);

                Transaction.Current.OnCommittedSuccessfully(() => connections.ForEach(subscriberConnection =>
                {
                    subscriberConnection.SendAsync(exactlyOnceEvent)
                                        .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, subscriberConnection.EndpointInformation.Id , exactlyOnceEvent.MessageId));
                }));
            }
        }

        public void SendTransactionally(MessageTypes.Remotable.ExactlyOnce.ICommand exactlyOnceCommand)
        {
            var connection = _transport.ConnectionToHandlerFor(exactlyOnceCommand);

            _storage.SaveMessage(exactlyOnceCommand, connection.EndpointInformation.Id);

            Transaction.Current.OnCommittedSuccessfully(() => connection.SendAsync(exactlyOnceCommand)
                                                                        .ContinueAsynchronouslyOnDefaultScheduler(task => HandleDeliveryTaskResults(task, connection.EndpointInformation.Id , exactlyOnceCommand.MessageId)));
        }

        void HandleDeliveryTaskResults(Task completedSendTask, EndpointId receiverId, Guid messageId)
        {
            if(completedSendTask.IsFaulted)
            {
                //Todo: Handle delivery failures sanely.
            } else
            {
                _storage.MarkAsReceived(messageId, receiverId);
            }
        }

        public async Task StartAsync()
        {

           var startingStorage = _configuration.IsPureClientEndpoint
                                  ? Task.CompletedTask
                                  : _storage.StartAsync();

           await startingStorage.NoMarshalling();
        }
    }
}
