using System;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Logging;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
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
            var store = new SqlServerDocumentDb(ServiceLocator.DocumentDbConnectionString());
            using (var session = new DocumentDbSession(store, new SingleThreadUseGuard()))
            {
                session.Save(new User() { Id = userId });
                session.SaveChanges();
            }
        }

        [Test]
        public void Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
        {
            var readingDocumentDb = CreateStore();

            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            using(ServiceLocator.BeginScope())
            using (var session = OpenSession(readingDocumentDb))
            {
                session.Get<User>(userId);
            }
        }

        [Test]
        public void Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance()
        {
            var readingDocumentDb = CreateStore();

            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            using (ServiceLocator.BeginScope())
            using (var session = OpenSession(readingDocumentDb))
            {
                session.GetAll<User>().Count().Should().Be(1);
            }
        }

        [Test]
        public void Can_get_all_documents_of_previously_unknown_class_added_by_onother_documentDb_instance_byId()
        {
            var readingDocumentDb = CreateStore();

            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherDocumentDb(userId);

            using (ServiceLocator.BeginScope())
            using (var session = OpenSession(readingDocumentDb))
            {
                session.Get<User>(Seq.Create(userId)).Count().Should().Be(1);
            }
        }
    }
}