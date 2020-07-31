using System;
using System.Collections.Generic;
using IBM.Data.DB2.Core;
using Composable.Persistence.Common;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2CommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this DB2Command @this, Func<DB2DataReader, T> select)
        {
            using var reader = @this.ExecuteReader();
            var result = new List<T>();
            reader.ForEachSuccessfulRead(row => result.Add(select(row)));
            return result;
        }
    }
}
