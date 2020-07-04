using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    interface IMySqlConnectionProvider
    {
        MySqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
