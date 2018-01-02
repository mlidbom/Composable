using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        internal class MessageStorage
        {
            internal async Task MarkAsSentAsync(TransportMessage.OutGoing outGoingMessage)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }

            internal async Task MarkAsReceivedAsync(TransportMessage.Response.Incoming response)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }

            public async Task SaveMessageAsync(ITransactionalExactlyOnceDeliveryMessage message)
            {
                try
                {
                    await Task.CompletedTask;
                }
                catch(Exception)
                {
                    //todo: proper exception handling here.
                    throw;
                }
            }
        }
    }
}
