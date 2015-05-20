using System;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Adds basic blocking of messages from reaching the synchronous bus. 
    /// Implementations are called each time a message is about to be dispatched to a handler. 
    /// Implementors should inspect the message and handler to decide whether the message should be dispatched. 
    /// </summary>
    [Obsolete("No longer used. Checkout IHandleRemoteMessages<> instead.", true)]
    public interface ISynchronousBusSubscriberFilter
    {
        bool PublishMessageToHandler(object message, object handler);
    }
}
