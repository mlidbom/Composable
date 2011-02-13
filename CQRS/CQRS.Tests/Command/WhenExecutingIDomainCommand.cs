using Composable.CQRS;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.ServiceLocatorAdapter;

namespace CQRS.Tests.Command
{
    [TestFixture]
    public class WhenExecutingIDomainCommand
    {
        [Test]
        public void CorrectCommandHandlerProviderIsUsed()
        {
            var registry = new Registry();
            var handler = new ModifyCandidateHandler();
            registry.For<ICommandHandler<ModifyCandidateCommand>>().Use(() => handler);

            var container = new Container(registry);
            var locator = new StructureMapServiceLocator(container);
            new Composable.CQRS.CommandService(locator).Execute(new ModifyCandidateCommand());
            Assert.That(handler.ExecuteCalled, Is.True, "Execute(ModifyCandidateCommand command) should have been called");
        }

        public class ModifyCandidateHandler : ICommandHandler<ModifyCandidateCommand>
        {
            public void Execute(ModifyCandidateCommand command)
            {
                ExecuteCalled = true;
            }

            public bool ExecuteCalled { get; private set; }
        }

        public class ModifyCandidateCommand
        {
        }
    }
}