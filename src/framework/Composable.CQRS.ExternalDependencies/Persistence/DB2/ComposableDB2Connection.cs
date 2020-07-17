using System;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2
{
    class ComposableDB2Connection : IDisposable, IAsyncDisposable
    {
        readonly DB2Connection _connection;
        public ComposableDB2Connection(string connectionString) => _connection = new DB2Connection(connectionString);

        internal DB2Connection Connection => _connection;

        public void Open()
        {
            _connection.Open();;
        }

        public DB2Command CreateCommand()
        {
            return _connection.CreateCommand();
        }

        public void Dispose() => _connection.Dispose();

        public ValueTask DisposeAsync() => _connection.DisposeAsync();
    }
}
