using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    //urgent: This looks like the storage for an outbox. Where is our outbox? Why can't i identify it clearly in code. It makes for confusion when the inbox doesn't have a corresponding outbox.
    partial class InterprocessTransport
    {
        public interface IMessageStorage
        {
            void SaveMessage(MessageTypes.Remotable.ExactlyOnce.IMessage message, params EndpointId[] receiverEndpointIds);
            void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId);
            Task StartAsync();
        }
    }
}
