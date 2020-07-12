using System;
using System.Data;
using System.Linq;
using System.Threading;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.PersistenceLayer;
using Composable.Persistence.MySql.SystemExtensions;
using Composable.Persistence.MySql.Testing.Databases;
using Composable.Persistence.MsSql.SystemExtensions;
using Composable.Persistence.MsSql.Testing.Databases;
using Composable.Persistence.Oracle.SystemExtensions;
using Composable.Persistence.Oracle.Testing.Databases;
using Composable.Persistence.PgSql.SystemExtensions;
using Composable.Persistence.PgSql.Testing.Databases;
using Composable.System.Threading;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using NUnit.Framework;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace Composable.Tests.ExternalDependencies
{
    //Urgent: Write tests that verify that none of the persistence layers lose precision in the persisted readorder when persisting refactorings.
    //Urgent: Remove this once we have all the persistence layers working.
    [TestFixture] public class ExplorePersistenceLayerAdoImplementations
    {
        MsSqlDatabasePool _msSqlPool;
        MySqlDatabasePool _mySqlPool;
        MsSqlConnectionProvider _msSqlConnection;
        MySqlConnectionProvider _mySqlConnection;
        PgSqlDatabasePool _pgSqlPool;
        PgSqlConnectionProvider _pgSqlConnection;

        OracleDatabasePool _orclPool;
        OracleConnectionProvider _orclConnection;

        [Test] public void MsSqlRoundtrip()
        {
            var result = _msSqlConnection.UseCommand(
                command => command.SetCommandText("select @parm")
                                  .AddNullableParameter("parm", SqlDbType.Decimal, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToSqlDecimal())
                                  .ExecuteReaderAndSelect(@this => @this.GetSqlDecimal(0))
                                  .Single());

            Console.WriteLine(result.ToString());
        }

        [Test] public void MySqlRoundtrip()
        {
            var result = _mySqlConnection.UseCommand(
                command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                  .AddNullableParameter("parm", MySqlDbType.VarChar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                  .ExecuteReaderAndSelect(@this => @this.GetString(0))
                                  .Single());

            Console.WriteLine(result);
        }

        [Test] public void PgSqlRoundtrip()
        {
            var result = _pgSqlConnection.UseCommand(
                command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                  .AddNullableParameter("parm", NpgsqlDbType.Varchar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                  .ExecuteReaderAndSelect(@this => @this.GetString(0))
                                  .Single());

            Console.WriteLine(result);
        }

        [Test] public void OracleRoundtrip()
        {
            var result2 = _orclConnection.UseCommand(
                command => command.SetCommandText("select :parm from dual")
                                  .AddNullableParameter("parm", OracleDbType.Decimal, OracleDecimal.Parse("1"))
                                  .ExecuteReaderAndSelect(@this => @this.GetOracleDecimal(0))
                                  .Single());

            Console.WriteLine(result2);
            Console.WriteLine(result2.ToReadOrder());
        }

        [SetUp] public void SetupTask()
        {
            _msSqlPool = new MsSqlDatabasePool();
            _msSqlConnection = new MsSqlConnectionProvider(_msSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _mySqlPool = new MySqlDatabasePool();
            _mySqlConnection = new MySqlConnectionProvider(_mySqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _pgSqlPool = new PgSqlDatabasePool();
            _pgSqlConnection = new PgSqlConnectionProvider(_pgSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            _orclPool = new OracleDatabasePool();
            _orclConnection = new OracleConnectionProvider(_orclPool.ConnectionStringFor(Guid.NewGuid().ToString()));
        }

        [TearDown] public void TearDownTask()
        {
            _msSqlPool.Dispose();
            _mySqlPool.Dispose();
            _pgSqlPool.Dispose();
            _orclPool.Dispose();
        }

        static ReadOrder Create(long order, long offset) => ReadOrder.Parse($"{order}.{offset:D19}");
        static string CreateString(int order, int value) => $"{order}.{DecimalPlaces(value)}";
        static string DecimalPlaces(int number) => new string(number.ToString()[0], 19);
    }
}
