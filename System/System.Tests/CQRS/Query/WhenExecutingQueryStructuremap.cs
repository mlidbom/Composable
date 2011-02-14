using System;
using System.Collections.Generic;
using Composable.CQRS.Query;
using Microsoft.Practices.ServiceLocation;
using NUnit.Framework;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.ServiceLocatorAdapter;

namespace CQRS.Tests.Query
{
    [TestFixture]
    public class WhenExecutingQueryStructuremap : WhenExecutingQuery
    {
        private StructureMapServiceLocator _locator;
        protected override IServiceLocator Locator { get { return _locator; } }

        [SetUp]
        public void Setup()
        {
            var registry = new Registry();
            

            registry.Scan(scanner =>
            {
                scanner.AssemblyContainingType<ActiveCandidatesHandler>();
                scanner.ConnectImplementationsToTypesClosing(typeof(IQueryHandler<,>));
            });
            registry.For<IServiceLocator>().Use( () => _locator);
            registry.For<IQueryService>().Use<QueryService>();

            var container = new Container(registry);
            _locator = new StructureMapServiceLocator(container);
        }        
    }
}