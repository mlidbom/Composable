using System;
using System.Collections.Generic;
using Composable.CQRS;
using Composable.DDD;
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
            var candidateProvider = new EntityHandlerProvider();

            var candidate = new Candidate(Guid.Parse("32687730-5dd8-44e8-862f-a91d01912b29"));
            candidateProvider.Add(candidate);

            var registry = new Registry();
            registry.For<IEntityCommandHandlerProvider>().Use(()=> candidateProvider);

            var container = new Container(registry);
            var locator = new StructureMapServiceLocator(container);
            new Composable.CQRS.CommandService(locator).Execute(new ModifyCandidateCommand(candidate));
            Assert.That(candidate.ModifyCandidateCalled, Is.True, "Execute(ModifyCandidateCommand command) should have been called");
        }
    }

    public class Candidate : PersistentEntity<Candidate>, ICommandHandler<ModifyCandidateCommand>
    {
        public Candidate(Guid id) : base(id)
        {
        }

        public void Execute(ModifyCandidateCommand command)
        {
            ModifyCandidateCalled = true;
        }

        public bool ModifyCandidateCalled { get; private set; }
    }

    public class EntityHandlerProvider : IEntityCommandHandlerProvider
    {
        private readonly IDictionary<Guid, Candidate> _candidates = new Dictionary<Guid, Candidate>();

        public ICommandHandler<TCommand> Provide<TCommand>(TCommand command)
        {
            return (ICommandHandler<TCommand>) _candidates[(Guid) ((IEntityCommand)command).EntityId];
        }

        public void Add(Candidate candidate)
        {
            _candidates[candidate.Id] = candidate;
        }
    }

    public class ModifyCandidateCommand :  IEntityCommand
    {
        public ModifyCandidateCommand(Candidate candidate)
        {
            EntityId = candidate.Id;
        }

        public object EntityId { get; private set; }
    }
}