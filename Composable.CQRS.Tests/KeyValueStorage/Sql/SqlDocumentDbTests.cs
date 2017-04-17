using System;
using System.Linq;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    [Serializable]
    class SqlDocumentDbTests : DocumentDbTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.RealComponents);

        void InsertUsersInOtherDocumentDb(Guid userId)
        {
            using(var cloneServiceLocator = ServiceLocator.Clone())
            {
                cloneServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() => cloneServiceLocator.DocumentDbUpdater()
                                                                                    .Save(new User() {Id = userId}));
            }
        }

        [Test]
        public void Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            using(ServiceLocator.BeginScope())
            {
                ServiceLocator.DocumentDbSession().Get<User>(userId);
            }
        }

        [Test]
        public void Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance()
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            using (ServiceLocator.BeginScope())
            {
                ServiceLocator.DocumentDbSession().GetAll<User>().Count().Should().Be(1);
            }
        }

        [Test]
        public void Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance_byId()
        {
            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            UseInScope(reader => reader.Get<User>(Seq.Create(userId))
                                       .Count()
                                       .Should()
                                       .Be(1));
        }
    }
}