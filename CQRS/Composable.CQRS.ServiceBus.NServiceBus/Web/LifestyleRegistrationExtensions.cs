using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;
using Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb;

namespace Composable.CQRS.ServiceBus.NServiceBus.Web
{
    public static class LifestyleRegistrationExtensions
    {
        public static ComponentRegistration<S> PerNserviceBusMessage<S>(this LifestyleGroup<S> lifetLifestyleGroup)
        {
            return lifetLifestyleGroup.Custom<PerNserviceBusMessageLifestyleManager>();
        }

        public static ComponentRegistration<S> HybridPerWebRequestPerNserviceBusMessage<S>(this LifestyleGroup<S> lifestyleGroup)
        {
            return lifestyleGroup.Custom<HybridPerWebRequestPerNserviceBusMessageLifestyleManager>();
        }
    }
}