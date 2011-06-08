using Castle.MicroKernel.Registration;
using Composable.AutoMapper;
using Composable.CQRS.Query;
using Composable.StuffThatDoesNotBelongHere;

namespace Composable.CQRS.Windsor
{
    public static class WindsorExtensions
    {
        public static BasedOnDescriptor RegisterCommandHandlers(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(ICommandHandler<>)).WithService.Base().Configure(cfg => cfg.LifeStyle.Transient);
        }

        public static BasedOnDescriptor RegisterQueryHandlers(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(IQueryHandler<,>)).WithService.Base().Configure(cfg => cfg.LifeStyle.Transient);
        }

        public static BasedOnDescriptor RegisterEventPersisters(this FromAssemblyDescriptor me)
        {
            return me.BasedOn(typeof(IEventPersister<>)).WithService.Base().Configure(cfg => cfg.LifeStyle.Transient);
        }

        public static BasedOnDescriptor RegisterMappingProviders(this FromAssemblyDescriptor me)
        {
            return me.BasedOn<IProvidesMappings>().WithService.Base();
        }
    }
}