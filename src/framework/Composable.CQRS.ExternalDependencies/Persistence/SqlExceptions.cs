using System.Data.SqlClient;
using System.Linq;
using Composable.System;
using IBM.Data.DB2.Core;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence
{
    //We will most likely want to make higher level policy based on this information, so let's start concentrating it here rather than spreading it everywhere.
    static class SqlExceptions
    {
        internal static class MsSql
        {
            const int PrimaryKeyViolationSqlErrorNumber = 2627;
            internal static bool IsPrimaryKeyViolation(SqlException e) => e.Number == PrimaryKeyViolationSqlErrorNumber;
        }

        internal static class DB2
        {
            const string DeadlockOrTimeout = "40001";
            internal static bool IsDeadlockOrTimeOut(DB2Exception exception) => exception.Errors.Cast<DB2Error>().Any(error => error.SQLState == DeadlockOrTimeout);

            const string PrimaryKeyViolationSqlState = "23505";
            internal static bool IsPrimaryKeyViolation(DB2Exception exception) => (exception.Errors.Cast<DB2Error>().Any(error => error.SQLState == PrimaryKeyViolationSqlState));
        }

        internal static class Oracle
        {
            //Urgent: Implement this
            internal static bool IsPrimaryKeyViolationTODO(OracleException e) => false;
        }

        internal static class MySql
        {
            const int PrimaryKeyViolationSqlErrorNumber = 1062;
            internal static bool IsPrimaryKeyViolation(MySqlException e) => (e.Data["Server Error Code"] as int?) == PrimaryKeyViolationSqlErrorNumber;
        }

        internal static class PgSql
        {
            const int PrimaryKeyViolationSqlErrorNumber = 23505;
            internal static bool IsPrimaryKeyViolation(PostgresException e) => e.SqlState == PrimaryKeyViolationSqlErrorNumber.ToStringInvariant();
        }
    }
}
