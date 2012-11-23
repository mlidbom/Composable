using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace Composable.CQRS.Windsor
{
    public static class WindsorLifestyleRegistrationExtensions
    {
        /// <summary>
        /// Currently just an alias for Scoped since that is how we implement per message lifestyle in nservicebus.
        /// </summary>
        public static ComponentRegistration<TComponent> PerNserviceBusMessage<TComponent>(this LifestyleGroup<TComponent> lifestyleGroup) where TComponent : class
        {
            return lifestyleGroup.Scoped();
        }
    }
}