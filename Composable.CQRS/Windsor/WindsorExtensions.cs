using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;

namespace Composable.Windsor
{
    public static class WindsorExtensions
    {
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