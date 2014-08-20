using Castle.MicroKernel.Registration;
using Composable.CQRS.Query;

namespace Composable.CQRS.Windsor
{
    public static class WindsorExtensions
    {
        public static BasedOnDescriptor RegisterCommandHandlers(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(ICommandHandler<>)).WithService.Base().LifestyleTransient();
        }
    }
}