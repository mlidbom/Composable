using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary> Defines a message handler that should only listen for remote (the "real" bus) messages.</summary>
    /// <remarks> Will not be dispatched by <see cref="SynchronousBus"/></remarks>
    public interface IHandleRemoteMessages<T> : IHandleMessages<T> { }
}
