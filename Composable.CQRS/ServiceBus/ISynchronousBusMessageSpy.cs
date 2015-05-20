using NServiceBus;

namespace Composable.ServiceBus
{
    ///<summary>
    ///Implement this interface in order to be able to "Spy" on Sent messages without "handling" them.
    ///Implementers promise not to actually handle the message, but to only observe, log etc.
    /// </summary>
    public interface ISynchronousBusMessageSpy {}

    public interface ISynchronousBusMessageSpy<TMessage> : IHandleInProcessMessages<TMessage> where TMessage : IMessage {}
}
