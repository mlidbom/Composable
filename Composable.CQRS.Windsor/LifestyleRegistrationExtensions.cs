using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace Composable.CQRS.Windsor
{
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public static class WindsorLifestyleRegistrationExtensions
    {
        /// <summary>
        /// Currently just an alias for Scoped since that is how we implement per message lifestyle in nservicebus.
        /// </summary>
        [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
        public static ComponentRegistration<TComponent> PerNserviceBusMessage<TComponent>(this LifestyleGroup<TComponent> lifestyleGroup) where TComponent : class
        {
            return lifestyleGroup.Scoped();
        }
    }
}