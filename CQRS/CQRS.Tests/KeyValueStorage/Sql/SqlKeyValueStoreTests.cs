using System.Configuration;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    class SqlKeyValueStoreTests : KeyValueStoreTests
    {
        protected override IKeyValueStore CreateStore()
        {
            return new SqlServerKeyValueStore(ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString);
        }
    }
}