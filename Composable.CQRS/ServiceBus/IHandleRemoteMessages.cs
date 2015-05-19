using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary> Implement for message handlers that should only listen for remote (the "real" bus) messages.</summary>
    /// <remarks> Will not be dispatched by <see cref="SynchronousBus"/></remarks>
    public interface IHandleRemoteMessages<T> : IHandleMessages<T> { }
}
