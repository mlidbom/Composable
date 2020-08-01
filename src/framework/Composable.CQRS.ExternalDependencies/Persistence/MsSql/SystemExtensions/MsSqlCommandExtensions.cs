using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Composable.Persistence.Common.AdoCE;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    static class MsSqlCommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
            => DbCommandCE.ExecuteReaderAndSelect(@this, select);
    }
}
