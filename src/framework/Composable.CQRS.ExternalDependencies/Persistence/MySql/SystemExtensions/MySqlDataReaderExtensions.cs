using System;
using MySql.Data.MySqlClient;

namespace Composable.Persistence.MySql.SystemExtensions
{
    static class MySqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this MySqlDataReader @this, Action<MySqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
