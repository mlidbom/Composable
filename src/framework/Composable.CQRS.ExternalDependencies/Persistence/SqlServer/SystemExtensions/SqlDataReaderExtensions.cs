using System;
using System.Data.SqlClient;

namespace Composable.Persistence.SqlServer.SystemExtensions
{
    static class SqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this SqlDataReader @this, Action<SqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
