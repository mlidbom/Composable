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
            readonly Database _database;
            readonly SqlServerDatabasePool _pool;
            readonly SqlServerConnectionProvider _innerProvider;
            public ConnectionProvider(Database database, SqlServerDatabasePool pool)
            {
                _database = database;
                _pool = pool;
                _innerProvider = new SqlServerConnectionProvider(ConnectionString);
            }

            public string ConnectionString => _database.ConnectionString(_pool);

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
