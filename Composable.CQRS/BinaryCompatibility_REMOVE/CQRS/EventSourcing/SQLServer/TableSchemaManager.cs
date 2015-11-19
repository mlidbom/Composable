using System;
using System.Data.SqlClient;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    [Obsolete("Search and replace: 'using Composable.CQRS.EventSourcing.SQLServer;' with 'using Composable.CQRS.EventSourcing.MicrosoftSQLServer;' this type is only still around for binary compatibility.", error: true)]
    internal abstract class TableSchemaManager
    {
        public abstract string Name { get; }
        public abstract string CreateTableSql { get; }

        public void DropIfExists(SqlConnection connection)
        {
            using (var dropCommand = connection.CreateCommand())
            {
                dropCommand.CommandText =
                    $@"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{Name}]') AND type in (N'U'))
DROP TABLE [dbo].[{Name}]";

                dropCommand.ExecuteNonQuery();
            }
        }

        public bool Exists(SqlConnection connection)
        {
            using (var checkForTableCommand = connection.CreateCommand())
            {
                checkForTableCommand.CommandText = $"select count(*) from sys.tables where name = '{Name}'";
                return 1 == (int)checkForTableCommand.ExecuteScalar();
            }
        }

        public void Create(SqlConnection connection)
        {
            ExecuteNonQuery(connection, CreateTableSql);
        }

        private void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (var createTableCommand = connection.CreateCommand())
            {
                createTableCommand.CommandText = sql;
                createTableCommand.ExecuteNonQuery();
            }
        }

    }

}