using System.Configuration;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    class SqlKeyValueStoreTests : KeyValueStoreTests
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        
        [SetUp]
        public static void Setup()
        {
            SqlServerKeyValueStore.ResetDB(connectionString);
        }

        protected override IKeyValueStore CreateStore()
        {
            return new SqlServerKeyValueStore(connectionString);
        }
    }
}