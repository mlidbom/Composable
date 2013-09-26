#region usings

using System.Collections.Generic;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS;
using Composable.CQRS.Command;
using Composable.DDD;
using Composable.DomainEvents;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
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
        private IWindsorContainer _container;

        private static readonly IList<SomethingHappened> _raisedEvents =
            new List<SomethingHappened>
                {
                    new SomethingHappened { Name = "FirstEvent" },
                    new SomethingHappened { Name = "SecondEvent" }
                };

        [SetUp]
        public void Setup()
        {

            _container = new WindsorContainer();
            _container.Register(
                Component.For<ICommandService>().ImplementedBy<CommandService>(),
                Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                Component.For<ICommandHandler<ModifyCandidateCommand>>().ImplementedBy<ModifyCandidateHandler>(),
                Component.For<IWindsorContainer>().Instance(_container)
                );           

            ModifyCandidateHandler.ExecuteCalled = false;
        }

        [Test]
        public void CorrectCommandHandlerIsUsed()
        {
            _container.Resolve<ICommandService>().Execute(new ModifyCandidateCommand());
            Assert.That(ModifyCandidateHandler.ExecuteCalled, Is.True, "ModifyCandidateHandler.Execute should have been called");
        }

        [Test]
        public void RaisedEventsAreReturnedInOrder()
        {
            var result = _container.Resolve<ICommandService>().Execute(new ModifyCandidateCommand());
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