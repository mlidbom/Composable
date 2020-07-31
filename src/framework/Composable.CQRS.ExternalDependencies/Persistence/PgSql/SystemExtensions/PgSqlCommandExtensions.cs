using System;
using System.Collections.Generic;
using Npgsql;
using Composable.Persistence.Common;

namespace Composable.Persistence.PgSql.SystemExtensions
{
    static class MyNpgsqlCommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this NpgsqlCommand @this, Func<NpgsqlDataReader, T> select) =>
            DbCommandCE.ExecuteReaderAndSelect(@this, select);
    }
}
