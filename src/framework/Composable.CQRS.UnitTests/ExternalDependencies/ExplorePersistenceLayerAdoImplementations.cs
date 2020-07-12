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

   //urgent: Fails on magnus-desktop with:
   //Oracle.ManagedDataAccess.Client.OracleException : ORA-01722: ogiltigt nummer
   //at OracleInternal.ServiceObjects.OracleConnectionImpl.VerifyExecution(Int32& cursorId, Boolean bThrowArrayBindRelatedErrors, SqlStatementType sqlStatementType, Int32 arrayBindCount, OracleException& exceptionForArrayBindDML, Boolean& hasMoreRowsInDB, Boolean bFirstIterationDone)
   //at OracleInternal.ServiceObjects.OracleCommandImpl.ExecuteReader(String commandText, OracleParameterCollection paramColl, CommandType commandType, OracleConnectionImpl connectionImpl, OracleDataReaderImpl& rdrImpl, Int32 longFetchSize, Int64 clientInitialLOBFS, OracleDependencyImpl orclDependencyImpl, Int64[] scnForExecution, Int64[]& scnFromExecution, OracleParameterCollection& bindByPositionParamColl, Boolean& bBindParamPresent, Int64& internalInitialLOBFS, OracleException& exceptionForArrayBindDML, OracleConnection connection, OracleLogicalTransaction& oracleLogicalTransaction, IEnumerable`1 adrianParsedStmt, Boolean isDescribeOnly, Boolean isFromEF)
   //at Oracle.ManagedDataAccess.Client.OracleCommand.ExecuteReader(Boolean requery, Boolean fillRequest, CommandBehavior behavior)
   //at Oracle.ManagedDataAccess.Client.OracleCommand.ExecuteReader()
   //at Composable.Persistence.Oracle.SystemExtensions.MyOracleCommandExtensions.ExecuteReaderAndSelect[T](OracleCommand this, Func`2 select) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.ExternalDependencies\Persistence\Oracle\SystemExtensions\OracleCommandExtensions.cs:line 51
   //at Composable.Tests.ExternalDependencies.ExplorePersistenceLayerAdoImplementations.<>c.<OracleRoundtrip>b__11_0(OracleCommand command) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.UnitTests\ExternalDependencies\ExplorePersistenceLayerAdoImplementations.cs:line 73
   //at Composable.Persistence.Oracle.SystemExtensions.MyOracleConnectionExtensions.UseCommand[TResult](OracleConnection this, Func`2 action) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.ExternalDependencies\Persistence\Oracle\SystemExtensions\OracleConnectionExtensions.cs:line 20
   //at Composable.Persistence.Oracle.SystemExtensions.MyOracleConnectionProviderExtensions.<>c__DisplayClass5_0`1.<UseCommand>b__0(OracleConnection connection) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.ExternalDependencies\Persistence\Oracle\SystemExtensions\OracleConnectionProviderExtensions.cs:line 20
   //at Composable.Persistence.Oracle.SystemExtensions.OracleConnectionProvider.UseConnection[TResult](Func`2 func) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.ExternalDependencies\Persistence\Oracle\SystemExtensions\OracleConnectionProvider.cs:line 42
   //at Composable.Persistence.Oracle.SystemExtensions.MyOracleConnectionProviderExtensions.UseCommand[TResult](IOracleConnectionProvider this, Func`2 action) in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.ExternalDependencies\Persistence\Oracle\SystemExtensions\OracleConnectionProviderExtensions.cs:line 20
   //at Composable.Tests.ExternalDependencies.ExplorePersistenceLayerAdoImplementations.OracleRoundtrip() in C:\Users\magnu\source\repos\Composable\src\framework\Composable.CQRS.UnitTests\ExternalDependencies\ExplorePersistenceLayerAdoImplementations.cs:line 72

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
        }

        static ReadOrder Create(long order, long offset) => ReadOrder.Parse($"{order}.{offset:D19}");
        static string CreateString(int order, int value) => $"{order}.{DecimalPlaces(value)}";
        static string DecimalPlaces(int number) => new string(number.ToString()[0], 19);
    }
}
