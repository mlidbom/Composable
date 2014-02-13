using NServiceBus.Saga;

namespace Composable.CQRS.ServiceBus.NServiceBus
{
    public interface IAmTimeoutMessage:ITimeoutState
    {
        string EnvironmentName { get; set; }
    }
}
