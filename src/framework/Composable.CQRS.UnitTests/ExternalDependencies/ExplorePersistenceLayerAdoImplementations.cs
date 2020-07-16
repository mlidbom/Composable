using System;
using System.Data;
using System.Linq;
using System.Threading;
using Composable.Persistence.Common.EventStore;
using Composable.Persistence.DB2.SystemExtensions;
using Composable.Persistence.DB2.Testing.Databases;
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
using FluentAssertions;
using IBM.Data.DB2.Core;
using IBM.Data.DB2Types;
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
        [Test] public void MsSqlRoundtrip()
        {
            using var msSqlPool = new MsSqlDatabasePool();
            var msSqlConnection = new MsSqlConnectionProvider(msSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            var result = msSqlConnection.UseCommand(
                action: command => command.SetCommandText(commandText: "select @parm")
                                          .AddNullableParameter(name: "parm", SqlDbType.Decimal, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToSqlDecimal())
                                          .ExecuteReaderAndSelect(@select: @this => @this.GetSqlDecimal(i: 0))
                                          .Single());

            Console.WriteLine(result.ToString());
        }

        [Test] public void MySqlRoundtrip()
        {
            using var mySqlPool = new MySqlDatabasePool();
            var mySqlConnection = new MySqlConnectionProvider(mySqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            var result = mySqlConnection.UseCommand(
                action: command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                          .AddNullableParameter(name: "parm", MySqlDbType.VarChar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                          .ExecuteReaderAndSelect(@select: @this => @this.GetString(i: 0))
                                          .Single());

            Console.WriteLine(result);
        }

        [Test] public void PgSqlRoundtrip()
        {
            using var pgSqlPool = new PgSqlDatabasePool();
            var pgSqlConnection = new PgSqlConnectionProvider(pgSqlPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            var result = pgSqlConnection.UseCommand(
                action: command => command.SetCommandText($"select cast(cast(@parm as {EventTable.ReadOrderType}) as char(39))")
                                          .AddNullableParameter(name: "parm", NpgsqlDbType.Varchar, ReadOrder.Parse($"{long.MaxValue}.{long.MaxValue}").ToString())
                                          .ExecuteReaderAndSelect(@select: @this => @this.GetString(ordinal: 0))
                                          .Single());

            Console.WriteLine(result);
        }

        [Test] public void OracleRoundtrip()
        {
            using var orclPool = new OracleDatabasePool();
            var orclConnection = new OracleConnectionProvider(orclPool.ConnectionStringFor(Guid.NewGuid().ToString()));

            var result2 = orclConnection.UseCommand(
                action: command => command.SetCommandText(commandText: "select :parm from dual")
                                          .AddNullableParameter(name: "parm", OracleDbType.Decimal, OracleDecimal.Parse(numStr: "1"))
                                          .ExecuteReaderAndSelect(@select: @this => @this.GetOracleDecimal(i: 0))
                                          .Single());

            Console.WriteLine(result2);
            Console.WriteLine(result2.ToReadOrder());
        }

        [Test] public void DB2Roundtrip()
        {
            var schema = "Composable_DatabasePool_0001";
            var db2Connection = new DB2ConnectionProvider($"SERVER=localhost;DATABASE=CDBPOOL;CurrentSchema={schema};User ID=db2admin;Password=Development!1;");

            var result2 = db2Connection.UseCommand(
                action: command => command.SetCommandText(commandText: "select cast(@parm as decimal(31,19)) from sysibm.sysdummy1")
                                          .AddParameter(name: "@parm", DB2Type.Decimal, DB2Decimal.Parse("1"))
                                          .ExecuteReaderAndSelect(@select: @this => @this.GetDB2Decimal(i: 0))
                                          .Single());

            Console.WriteLine(result2);
            Console.WriteLine(result2.ToReadOrder());
        }

        [Test] public void Db2Test()
        {
            var schema = "Composable_DatabasePool_0001";
            using var db2conn = new DB2Connection(connectionString: $"SERVER=localhost;DATABASE=CDBPOOL;CurrentSchema={schema};User ID=db2admin;Password=Development!1;");
            db2conn.Open();

            var cmd = db2conn.CreateCommand();
            cmd.CommandText = @"select current_schema from   sysibm.sysdummy1;";
            var result = (string)cmd.ExecuteScalar();
            result.Should().Be(schema.ToUpper());
        }

        [Test] public void DB2Test2()
        {
            var schema = "Composable_DatabasePool_0001";
            var db2Connection = new DB2ConnectionProvider($"SERVER=localhost;DATABASE=CDBPOOL;CurrentSchema={schema};User ID=db2admin;Password=Development!1;");

            db2Connection.UseCommand(cmd => cmd.SetCommandText($@"CALL DB2ADMIN.DROP_SCHEMA(@Name);")
                                                                        .AddParameter("Name", DB2Type.VarChar, schema)
                                                                        .ExecuteNonQuery());

        }

        static ReadOrder Create(long order, long offset) => ReadOrder.Parse($"{order}.{offset:D19}");
        static string CreateString(int order, int value) => $"{order}.{DecimalPlaces(value)}";
        static string DecimalPlaces(int number) => new string(number.ToString()[index: 0], count: 19);
    }
}
