#region usings

using System;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using CommonServiceLocator.WindsorAdapter;
using Composable.CQRS.Query;
using Composable.DDD;
using Microsoft.Practices.ServiceLocation;
using NUnit.Framework;

#endregion

namespace CQRS.Tests.Query
{
    [TestFixture]
    public class WhenExecutingQueryWindsor : WhenExecutingQuery
    {
        private WindsorServiceLocator _locator;
        protected override IServiceLocator Locator { get { return _locator; } }

        [SetUp]
        public void Setup()
        {
            var container = new WindsorContainer();
            _locator = new WindsorServiceLocator(container);

            container.Register(
                AllTypes.FromThisAssembly().BasedOn(typeof(IQueryHandler<,>)).WithService.Base(),
                Component.For<IQueryService>().ImplementedBy<QueryService>(),
                Component.For<IServiceLocator>().Instance(Locator)
                );
        }        
    }
}