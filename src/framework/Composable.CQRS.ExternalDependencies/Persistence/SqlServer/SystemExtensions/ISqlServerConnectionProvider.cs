using System.Data.SqlClient;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    interface ISqlServerConnectionProvider
    {
        SqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
