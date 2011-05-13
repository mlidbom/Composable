using Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb;

namespace Composable.CQRS.ServiceBus.NServiceBus.Web
{
    public class HybridPerWebRequestPerNserviceBusMessageLifestyleManager : HybridPerWebRequestLifestyleManager<PerNserviceBusMessageLifestyleManager> { }
}