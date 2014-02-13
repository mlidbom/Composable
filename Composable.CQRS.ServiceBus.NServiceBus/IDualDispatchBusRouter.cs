using Composable.ServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    /// <summary>
    /// <para>Decides whether a message should be dispatched to the <see cref="SynchronousBus"/> bus or the <see cref="NServiceBusServiceBus"/></para>
    /// If there is no <see cref="IDualDispatchBusRouter"/> registered in the container all messages that the synchronous bus can 
    /// handle will be dispatched to the <see cref="SynchronousBus"/>.
    /// </summary>
    public interface IDualDispatchBusRouter
    {
        /// <summary>
        /// <para>Implementors should return true if the message should be routed to the <see cref="SynchronousBus"/></para>
        /// The <see cref="DualDispatchBus"/> calls this method for each call to Send* to decide where to send the message.
        /// </summary>
        bool SendToSynchronousBus(object message, SynchronousBus syncBus);
    }
}