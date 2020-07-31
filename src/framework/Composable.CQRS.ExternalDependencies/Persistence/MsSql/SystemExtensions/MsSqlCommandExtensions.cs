using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Composable.Persistence.MsSql.SystemExtensions
{
    static class MsSqlCommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this SqlCommand @this, Func<SqlDataReader, T> select)
        {
            using var reader = @this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }
    }
}
