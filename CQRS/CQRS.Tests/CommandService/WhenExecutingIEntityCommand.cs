using System;
using Composable.CQRS;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.ServiceLocatorAdapter;

namespace CQRS.Tests.CommandService
{
    [TestFixture]
    public class WhenExecutingIEntityCommand
    {
        [Test]
        public void CorrectCommandHandlerProviderIsUsed()
        {
            var registry = new Registry();
            registry.For<IEntityCommandHandlerProvider>().Use<EntityHandlerProvider>();

            var container = new Container(registry);
            var locator = new StructureMapServiceLocator(container);
            new Composable.CQRS.CommandService(locator);
        }
    }

    public class EntityHandlerProvider : IEntityCommandHandlerProvider
    {
        public ICommandHandler<TCommand> Provide<TCommand, TEntityId>(TCommand command) where TCommand : IEntityCommand<TEntityId>
        {
            throw new NotImplementedException();
        }
    }

    public class DoSomethingCommand :  IEntityCommand
    {
        public Guid EntityId
        {
            get { return Guid.Parse("32687730-5dd8-44e8-862f-a91d01912b29"); }
        }
    }
}