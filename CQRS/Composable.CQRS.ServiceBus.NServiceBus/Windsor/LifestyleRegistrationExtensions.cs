using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace Composable.CQRS.ServiceBus.NServiceBus.Windsor
{
    public static class LifestyleRegistrationExtensions
    {
        private static bool ThisIsATestProject = false;
        private static bool EverCalled = false;

        public static void ThisIsATestProjectOnlyUseFromTests()
        {
            if(EverCalled && !ThisIsATestProject)
            {
                throw new Exception("Extensionmethods have already been used. This must be called FIRST");
            }
            ThisIsATestProject = true;
        }

        public static ComponentRegistration<S> PerNserviceBusMessage<S>(this LifestyleGroup<S> lifetLifestyleGroup) where S : class
        {
            EverCalled = true;
            return ThisIsATestProject ? lifetLifestyleGroup.Singleton : lifetLifestyleGroup.Transient;
        }
    }
}