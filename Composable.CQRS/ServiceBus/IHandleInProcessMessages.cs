
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary> Implement for message handlers that should only listen for messages dispatched by  <see cref="SynchronousBus"/>.</summary>
    public interface IHandleInProcessMessages<T> where T:IMessage
    {
        void Handle(T message);
    }
}
