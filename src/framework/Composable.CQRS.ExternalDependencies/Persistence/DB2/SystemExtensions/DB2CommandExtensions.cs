using System;
using System.Collections.Generic;
using IBM.Data.DB2.Core;
using Composable.Persistence.Common.AdoCE;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2CommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this DB2Command @this, Func<DB2DataReader, T> select)
            => DbCommandCE.ExecuteReaderAndSelect(@this, select);
    }
}
