using System;
using System.Data.SqlClient;
using Composable.System.Data.SqlClient;

namespace Composable.Testing.Databases
{
    sealed partial class SqlServerDatabasePool
    {
        class Connection : ISqlConnection
        {
            readonly Database _database;
            readonly string _reservationName;
            readonly SqlServerDatabasePool _pool;
            readonly ISqlConnection _inner;
            public Connection(Database database, string reservationName, SqlServerDatabasePool pool)
            {
                _database = database;
                _reservationName = reservationName;
                _pool = pool;
                _inner = new SqlServerConnection(ConnectionString);
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
                    return _inner.OpenConnection();
                }
                catch(Exception exception)
                {
                    _pool.ScheduleForRebooting();
                    throw new Exception("Dbpool was not working and is being rebooted. Please run your tests again.", exception);
                }
            }
        }
    }
}
