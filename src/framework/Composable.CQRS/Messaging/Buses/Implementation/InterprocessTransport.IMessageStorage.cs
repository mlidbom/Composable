using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
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
