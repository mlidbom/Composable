using System;
using System.Collections.Generic;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        class Database
        {
            internal int Id { get; }
            internal string Name { get; }
            internal bool IsFree { get; }
            internal string ConnectionString { get; }
            internal Database(SqlServerDatabasePool pool, int id, bool isFree)
            {
                Id = id;
                Name = $"{ManagerDbName}_{id:0000}";
                IsFree = isFree;
                ConnectionString = pool.ConnectionStringForDbNamed(Name);
            }
        }

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
            if(!ManagerDbExists())
            {
                CreateDatabase(ManagerDbName);
                _managerConnection.ExecuteNonQuery(CreateDbTableSql);
                ConnectionStringsWithKnownManagerDb.Add(_masterConnectionString);
            }
        }

        static class ManagerTableSchema
        {
            public static readonly string TableName = "Databases";
            public static readonly string Id = nameof(Id);
            public static readonly string IsFree = nameof(IsFree);
            public static readonly string ReservationDate = nameof(ReservationDate);
            public static readonly string ReservationCallStack = nameof(ReservationCallStack);
        }

        static readonly string CreateDbTableSql = $@"
CREATE TABLE [dbo].[{ManagerTableSchema.TableName}](
    [{ManagerTableSchema.Id}] [int] IDENTITY(1,1) NOT NULL,
	[{ManagerTableSchema.IsFree}] [bit] NOT NULL,
    [{ManagerTableSchema.ReservationDate}] [datetime] NOT NULL,
    [{ManagerTableSchema.ReservationCallStack}] [varchar](max) NOT NULL,
 CONSTRAINT [PK_DataBases] PRIMARY KEY CLUSTERED 
(
	[{ManagerTableSchema.Id}] ASC
))
";

        void DropAllAndStartOver()
        {
            _managerConnection.ClearConnectionPool();
            var dbsToDrop = new List<string>();
            _masterConnection.UseCommand(
                action: command =>
                        {
                            command.CommandText = "select name from sysdatabases";
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    var dbName = reader.GetString(i: 0);
                                    if (dbName.StartsWith(ManagerDbName))
                                        dbsToDrop.Add(dbName);
                                }
                            }
                        });

            foreach (var db in dbsToDrop)
            {
                var dropCommand = $"drop database [{db}]";
                //SafeConsole.WriteLine(dropCommand);
                try
                {
                    _masterConnection.ExecuteNonQuery(dropCommand);
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                }
            }

            ConnectionStringsWithKnownManagerDb.Clear();
        }

        internal static void DropAllAndStartOver(string masterConnectionString)
        {
            using(var pool = new SqlServerDatabasePool(masterConnectionString))
            {
                pool.DropAllAndStartOver();
            }
        }
    }
}