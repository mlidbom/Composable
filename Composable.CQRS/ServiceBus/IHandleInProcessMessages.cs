
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary> Should be implemented by message handlers that should only listen for messages dispatched by  <see cref="SynchronousBus"/>.</summary>
    public interface IHandleInProcessMessages<T> where T : IMessage
    {
        void Handle(T message);
    }
}
