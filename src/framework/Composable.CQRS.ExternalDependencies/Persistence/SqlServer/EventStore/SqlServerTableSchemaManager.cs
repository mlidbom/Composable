﻿using System.Data.SqlClient;

namespace Composable.Persistence.SqlServer.EventStore
{
    abstract class SqlServerTableSchemaManager
    {
        internal abstract string Name { get; }
        internal abstract string CreateTableSql { get; }

        public bool Exists(SqlConnection connection)
        {
            using var checkForTableCommand = connection.CreateCommand();
            checkForTableCommand.CommandText = $"select count(*) from sys.tables where name = '{Name}'";
            return 1 == (int)checkForTableCommand.ExecuteScalar();
        }

        public void Create(SqlConnection connection)
        {
            ExecuteNonQuery(connection, CreateTableSql);
        }

        static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using var createTableCommand = connection.CreateCommand();
            createTableCommand.CommandText = sql;
            createTableCommand.ExecuteNonQuery();
        }

    }

}