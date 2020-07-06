using System;
using System.Data;
using System.Linq;
using Composable.Persistence.EventStore;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Persistence.SqlServer.Testing.Databases;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace Composable.Tests.ExternalDependencies
{
    //Urgent: Remove this once mysql support is working.
    [TestFixture] public class ExploreMySqlDecimalSupport
    {
        SqlServerDatabasePool _msSqlPool;
        MySqlDatabasePool _mySqlPool;
        SqlServerConnectionProvider _msSqlConnection;
        MySqlConnectionProvider _mySqlConnection;

        [Test] public void MsSqlRoundtrip()
        {
            var result = _msSqlConnection.UseCommand(
                command => command.SetCommandText("select @parm")
                                  .AddNullableParameter("parm", SqlDbType.Decimal, IEventStorePersistenceLayer.ReadOrder.Parse($"{long.MaxValue.ToString()}.{long.MaxValue.ToString()}").ToSqlDecimal())
                                  .ExecuteReaderAndSelect(@this => @this.GetSqlDecimal(0))
                                  .Single());

            Console.WriteLine(result.ToString());
        }

        [Test] public void MySqlRoundtrip()
        {
            var result = _mySqlConnection.UseCommand(
                command => command.SetCommandText("select cast(cast(@parm as decimal(38,19)) as char(39))")
                                  .AddNullableParameter("parm", MySqlDbType.VarChar, IEventStorePersistenceLayer.ReadOrder.Parse($"{long.MaxValue.ToString()}.{long.MaxValue.ToString()}").ToSqlDecimal())
                                  .ExecuteReaderAndSelect(@this => @this.GetString(0))
                                  .Single());

            Console.WriteLine(result);
        }

        [SetUp] public void SetupTask()
        {
            _msSqlPool = new SqlServerDatabasePool();
            _msSqlConnection = new SqlServerConnectionProvider(_msSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _mySqlPool = new MySqlDatabasePool();
            _mySqlConnection = new MySqlConnectionProvider(_mySqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));
        }

        [TearDown] public void TearDownTask()
        {
            _msSqlPool.Dispose();
            _mySqlPool.Dispose();
        }
    }
}
