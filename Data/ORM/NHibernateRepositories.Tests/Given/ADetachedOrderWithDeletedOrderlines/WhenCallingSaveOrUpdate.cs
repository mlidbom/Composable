using System.Collections.Generic;
using Composable.Data.ORM.NHibernate;
using Composable.Data.ORM.NHibernateRepositories.Tests.Domain;
using NHibernate.ByteCode.Castle;
using NUnit.Framework;

namespace Composable.Data.ORM.NHibernateRepositories.Tests.Given.ADetachedOrderWithDeletedOrderlines
{
    [TestFixture]
    public class WhenCallingSaveOrUpdate : NhibernateRepositoryTest
    {
        [Test]
        public void TheDeletedOrderLinesShouldBeRemovedFromTheDatabase()
        {
            Order order = new Order
                          {
                              Lines = new List<OrderLine>
                                      {
                                          new OrderLine(),
                                          new OrderLine(),
                                          new OrderLine(),
                                          new OrderLine()
                                      }
                          };

            using (var session = new InMemoryNHibernatePersistenceSession<ProxyFactoryFactory>())
            {
                var repo = new TransactionalRepository<Order, int>(session);
                repo.SaveOrUpdate(order);

                session.Clear();

                order.Lines.RemoveAt(2);

                repo.SaveOrUpdate(order);

                session.Clear();

                var loadedOrder = repo.Get(order.PersistentId);
                Assert.That(order.Lines, Is.EquivalentTo(loadedOrder.Lines));
            }
        }
    }
}