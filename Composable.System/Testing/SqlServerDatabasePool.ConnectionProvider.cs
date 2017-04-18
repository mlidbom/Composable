using System;
using System.Data.SqlClient;
using Composable.Logging;
using Composable.System.Data.SqlClient;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool
    {
        class ConnectionProvider : ISqlConnectionProvider
        {
            readonly Database _database;
            readonly string _reservationName;
            readonly SqlServerDatabasePool _pool;
            readonly ISqlConnectionProvider _innerProvider;
            public ConnectionProvider(Database database, string reservationName, SqlServerDatabasePool pool)
            {
                _database = database;
                _reservationName = reservationName;
                _pool = pool;
                _innerProvider = new SqlServerConnectionProvider(ConnectionString);
            }

            public string ConnectionString => _database.ConnectionString(_pool);

            public SqlConnection OpenConnection()
            {
                if (!_database.IsReserved || _reservationName != _database.ReservationName)
                {
                    throw new Exception("Db ownership has somehow been lost. The pool is probably corrupt and beeing rebooted. Try rerunning your tests");
                }

                try
                {
                    return _innerProvider.OpenConnection();
                }
                catch(Exception exception)
                {
                    _pool.RebootPoolIfNotAlreadyRebooted();
                    throw new Exception("Dbpool was not working and is being rebooted. Please run your tests again.", exception);
                }
            }
        }
    }
}
