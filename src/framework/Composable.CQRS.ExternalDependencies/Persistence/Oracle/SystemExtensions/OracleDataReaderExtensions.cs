using System;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleDataReaderExtensions
    {
        public static void ForEachSuccessfulRead(this OracleDataReader @this, Action<OracleDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
