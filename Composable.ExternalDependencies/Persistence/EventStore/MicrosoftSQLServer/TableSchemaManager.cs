using System.Data.SqlClient;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    abstract class TableSchemaManager
    {
        protected abstract string Name { get; }
        protected abstract string CreateTableSql { get; }

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

        static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (var createTableCommand = connection.CreateCommand())
            {
                createTableCommand.CommandText = sql;
                createTableCommand.ExecuteNonQuery();
            }
        }

    }

}