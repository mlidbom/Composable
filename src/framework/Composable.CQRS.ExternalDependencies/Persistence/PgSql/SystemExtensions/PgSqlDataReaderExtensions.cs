using System;
using Npgsql;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class NpgsqlDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this NpgsqlDataReader @this, Action<NpgsqlDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
