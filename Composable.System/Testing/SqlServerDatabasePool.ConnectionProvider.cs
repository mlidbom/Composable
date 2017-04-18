using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Composable.System;
using Composable.System.Data.SqlClient;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        class ConnectionProvider : ISqlConnectionProvider
        {
            readonly SqlServerDatabasePool _pool;
            readonly SqlServerConnectionProvider _innerProvider;
            public ConnectionProvider(Database database, SqlServerDatabasePool pool)
            {
                _pool = pool;
                _innerProvider = new SqlServerConnectionProvider(database.ConnectionString(pool));
            }
            public SqlConnection OpenConnection()
            {
                try
                {
                    return _innerProvider.OpenConnection();
                }
                catch(Exception)
                {
                    _pool.DropAllAndStartOver();
                    throw;
                }
            }
        }
    }
}
