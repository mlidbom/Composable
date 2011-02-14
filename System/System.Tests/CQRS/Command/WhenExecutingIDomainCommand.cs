#region usings

using System.Collections.Generic;
using System.Linq;
using Castle.Core;
using Composable.CQRS;
using Composable.CQRS.Command;
using Composable.DDD;
using Composable.DomainEvents;
using Microsoft.Practices.ServiceLocation;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.ServiceLocatorAdapter;

#endregion

namespace CQRS.Tests.Command
{
    [TestFixture]
    public class WhenExecutingIDomainCommand
    {
        private StructureMapServiceLocator locator;

        private static readonly IList<SomethingHappened> _raisedEvents =
            new List<SomethingHappened>
                {
                    new SomethingHappened(){Name = "FirstEvent"},
                    new SomethingHappened(){Name = "SecondEvent"}
                };

        [SetUp]
        public void Setup()
        {
            var registry = new Registry();
            var handler = new ModifyCandidateHandler();
            registry.For<ICommandService>().Use<CommandService>();
            registry.For<ICommandHandler<ModifyCandidateCommand>>().Use<ModifyCandidateHandler>();
            registry.For<IServiceLocator>().Use(() => locator);

            var container = new Container(registry);
            locator = new StructureMapServiceLocator(container);
            ModifyCandidateHandler.ExecuteCalled = false;

            DomainEvent.Init(locator);
        }

        [TearDown]
        public void TearDown()
        {
            DomainEvent.ResetOnlyUseFromTests();
        }

        [Test]
        public void CorrectCommandHandlerIsUsed()
        {
            locator.GetInstance<ICommandService>().Execute(new ModifyCandidateCommand());
            Assert.That(ModifyCandidateHandler.ExecuteCalled, Is.True, "ModifyCandidateHandler.Execute should have been called");
        }

        [Test]
        public void RaisedEventsAreReturnedInOrder()
        {
            var result = locator.GetInstance<ICommandService>().Execute(new ModifyCandidateCommand());
            Assert.That(result.Events, Is.EqualTo(_raisedEvents));
        }


        public class ModifyCandidateHandler : ICommandHandler<ModifyCandidateCommand>
        {
            public void Execute(ModifyCandidateCommand command)
            {
                ExecuteCalled = true;
                _raisedEvents.ForEach(DomainEvent.Raise);
            }

            public static bool ExecuteCalled { get; set; }
        }

        public class ModifyCandidateCommand
        {
        }

        public class SomethingHappened : ValueObject<SomethingHappened>, IDomainEvent
        {
            public string Name;
        }
    }
}