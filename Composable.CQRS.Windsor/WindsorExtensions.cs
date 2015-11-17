using System;
using Castle.MicroKernel.Registration;
using Composable.CQRS.Query;

namespace Composable.CQRS.Windsor
{
    [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
    public static class WindsorExtensions
    {
        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package", error: true)]
        public static BasedOnDescriptor RegisterCommandHandlers(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(ICommandHandler<>)).WithService.Base().LifestyleTransient();
        }
    }
}