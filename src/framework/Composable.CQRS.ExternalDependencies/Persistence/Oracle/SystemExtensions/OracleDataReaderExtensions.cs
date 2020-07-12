using System;
using Oracle.ManagedDataAccess.Client;

namespace Composable.Persistence.Oracle.SystemExtensions
{
    static class OracleDataReaderExtensions
    {
        //Urgent: In all persistence layers, replace all manual implementation of this with an extension like this one.
        public static Guid GetGuidFromString(this OracleDataReader @this, int index) => Guid.Parse(@this.GetString(index));
        public static void ForEachSuccessfulRead(this OracleDataReader @this, Action<OracleDataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
