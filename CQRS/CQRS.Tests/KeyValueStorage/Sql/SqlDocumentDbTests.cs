using System;
using System.Configuration;
using System.Linq;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    [Serializable]
    [NCrunch.Framework.ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf)]
    class SqlDocumentDbTests : DocumentDbTests
    {
        private static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString; }
        }

            [SetUp]
        public static void Setup()
        {
            SqlServerDocumentDb.ResetDB(ConnectionString);
        }

        protected override IDocumentDb CreateStore()
        {
            return new SqlServerDocumentDb(ConnectionString);
        }


        [Serializable]
        public class InsertSomething : MarshalByRefObject
        {
            public void InsertSomeUsers(string connectionString, params Guid[] userIds)
            {
                var store = new SqlServerDocumentDb(connectionString);
                using(var session = OpenSession(store))
                {
                    Console.WriteLine(AppDomain.CurrentDomain.FriendlyName);
                    userIds.ForEach(userId => session.Save(new User(){Id = userId}));
                    session.SaveChanges();
                }
            }
        }
        
        public void InsertUsersInOtherAppDomain(Guid userIds)
        {
            var myDomain = AppDomain.CurrentDomain;
            var otherDomain = AppDomain.CreateDomain("other domain", myDomain.Evidence, myDomain.BaseDirectory, myDomain.RelativeSearchPath,false);

            var otherType = typeof(InsertSomething);
            var userInserter = otherDomain.CreateInstanceAndUnwrap(otherType.Assembly.FullName,otherType.FullName) as InsertSomething;

            userInserter.InsertSomeUsers(ConnectionString, userIds);
        }

        [Test]
        public void CanGetDocumentOfPreviouslyUnKnownClassAddedByAnotherDocumentDBInstance()
        {            
            var readingDocumentDb = CreateStore();

            var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            InsertUsersInOtherAppDomain(userId);

            using (var session = OpenSession(readingDocumentDb))
            {
                var loadedUser = session.Get<User>(userId);
            }
        }

        [Test]
        public void CanGetAllDocumentOfPreviouslyUnKnownClassAddedByAnotherDocumentDBInstance()
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
        public void CanGetAllDocumentOfPreviouslyUnKnownClassAddedByAnotherDocumentDBInstanceById()
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