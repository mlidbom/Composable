using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS;
using Composable.ServiceBus;

namespace Composable.Windsor
{
    public static class WindsorExtensions
    {
        public static BasedOnDescriptor RegisterCommandHandlers(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(ICommandHandler<>)).WithService.Base().LifestyleTransient();
        }

        public static IWindsorContainer RegisterMessageHandlersFromAssemblyContainingType<TAssemblyIdentifier>(this IWindsorContainer @this)
        {
            @this.Register(
                Classes.FromAssemblyContaining<TAssemblyIdentifier>()
                       .BasedOn(typeof(IHandleMessages<>))
                       .WithServiceBase()
                       .LifestyleScoped());

            return @this;
        }
    }
}