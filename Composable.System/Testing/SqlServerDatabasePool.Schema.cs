using System;
using System.Collections.Generic;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        static readonly HashSet<string> ConnectionStringsWithKnownManagerDb = new HashSet<string>();

        void SeparatelyInitConnectionPoolSoWeSeeRealisticExecutionTimesWhenProfiling() { _masterConnection.UseConnection(_ => { }); }

        void CreateDatabase(string databaseName)
        {
            _masterConnection.ExecuteNonQuery($"CREATE DATABASE [{databaseName}]");
            _masterConnection.ExecuteNonQuery($"ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;");
            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        bool ManagerDbExists()
        {
            if (!ConnectionStringsWithKnownManagerDb.Contains(_masterConnectionString))
            {
                SeparatelyInitConnectionPoolSoWeSeeRealisticExecutionTimesWhenProfiling();

                if (_masterConnection.ExecuteScalar($"select DB_ID('{ManagerDbName}')") == DBNull.Value)
                {
                    return false;
                }
            }

            ConnectionStringsWithKnownManagerDb.Add(_masterConnectionString);
            return true;
        }

        void EnsureManagerDbExists()
        {
            lock (typeof(SqlServerDatabasePool))
            {
                if (!ManagerDbExists())
                {
                    CreateDatabase(ManagerDbName);
                    _managerConnection.ExecuteNonQuery(CreateDbTableSql);
                    ConnectionStringsWithKnownManagerDb.Add(_masterConnectionString);
                }
            }
        }

        static class ManagerTableSchema
        {
            public static readonly string TableName = "Databases";
            public static readonly string Id = nameof(Id);
            public static readonly string DatabaseName = nameof(DatabaseName);
            public static readonly string IsFree = nameof(IsFree);
            public static readonly string ReservationDate = nameof(ReservationDate);
            public static readonly string ReservationCallStack = nameof(ReservationCallStack);
        }

        static readonly string CreateDbTableSql = $@"
CREATE TABLE [dbo].[{ManagerTableSchema.TableName}](
    [{ManagerTableSchema.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{ManagerTableSchema.DatabaseName}] [varchar](500) NOT NULL,
	[{ManagerTableSchema.IsFree}] [bit] NOT NULL,
    [{ManagerTableSchema.ReservationDate}] [datetime] NOT NULL,
    [{ManagerTableSchema.ReservationCallStack}] [varchar](max) NOT NULL,
 CONSTRAINT [PK_DataBases] PRIMARY KEY CLUSTERED 
(
	[{ManagerTableSchema.Id}] ASC
))
";
    }
}