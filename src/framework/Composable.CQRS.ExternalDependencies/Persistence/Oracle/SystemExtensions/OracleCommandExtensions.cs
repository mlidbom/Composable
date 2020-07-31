using System;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Composable.Persistence.Common;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleCommandExtensions
    {
        public static IReadOnlyList<T> ExecuteReaderAndSelect<T>(this OracleCommand @this, Func<OracleDataReader, T> select) =>
            DbCommandCE.ExecuteReaderAndSelect(@this, select);
    }
}
