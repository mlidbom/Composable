using System.Configuration;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    [NCrunch.Framework.ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf)]
    class SqlDocumentDbTests : DocumentDbTests
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        
        [SetUp]
        public static void Setup()
        {
            SqlServerObjectStore.ResetDB(connectionString);
        }

        protected override IObservableObjectStore CreateStore()
        {
            return new SqlServerObjectStore(connectionString);
        }
    }
}