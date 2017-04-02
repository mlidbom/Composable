using System;
using System.Configuration;
using System.Linq;
using Composable.Logging;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    [Serializable]
    class SqlDocumentDbTests : DocumentDbTests
    {
        static SqlServerDatabasePool _databasePool;

        string _connectionString;

        [SetUp]
        public void Setup()
        {
            var masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString;
            _databasePool = new SqlServerDatabasePool(masterConnectionString);
            _connectionString = _databasePool.ConnectionStringFor("SqlDocumentDbTests_DB");
        }

        [TearDown]
        public void TearDownTask()
        {
            _databasePool.Dispose();
        }

        protected override IDocumentDb CreateStore() => new SqlServerDocumentDb(_connectionString);

        [Serializable]
        class InsertSomething : MarshalByRefObject
        {
            public void InsertSomeUsers(string connectionString, params Guid[] userIds)
            {
                var store = new SqlServerDocumentDb(connectionString);
                using(var session = OpenSession(store))
                {
                    SafeConsole.WriteLine(AppDomain.CurrentDomain.FriendlyName);
                    userIds.ForEach(userId => session.Save(new User(){Id = userId}));
                    session.SaveChanges();
                }
            }
        }

        void InsertUsersInOtherAppDomain(Guid userIds)
        {
            var myDomain = AppDomain.CurrentDomain;
            var otherDomain = AppDomain.CreateDomain("other domain", myDomain.Evidence, myDomain.BaseDirectory, myDomain.RelativeSearchPath,false);

            var otherType = typeof(InsertSomething);
            var userInserter = otherDomain.CreateInstanceAndUnwrap(otherType.Assembly.FullName,otherType.FullName) as InsertSomething;

            userInserter.InsertSomeUsers(_connectionString, userIds);

            AppDomain.Unload(otherDomain);
        }

        [Test]
        public void Can_get_document_of_previously_unknown_class_added_by_onother_documentDb_instance()
        {
            var readingDocumentDb = CreateStore();

            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherAppDomain(userId);

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

            InsertUsersInOtherAppDomain(userId);

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

            InsertUsersInOtherAppDomain(userId);

            using (var session = OpenSession(readingDocumentDb))
            {
                session.Get<User>(Seq.Create(userId)).Count().Should().Be(1);
            }
        }
    }
}