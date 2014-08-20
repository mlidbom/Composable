using NServiceBus.Saga;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public interface IAmTimeoutMessage
    {
        string EnvironmentName { get; set; }
    }
}
