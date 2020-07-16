using System;
using IBM.Data.DB2.Core;

namespace Composable.Persistence.DB2.SystemExtensions
{
    static class DB2DataReaderExtensions
    {
        //Urgent: In all persistence layers, replace all manual implementation of this with an extension like this one.
        public static Guid GetGuidFromString(this DB2DataReader @this, int index) => Guid.Parse(@this.GetString(index));
        public static void ForEachSuccessfulRead(this DB2DataReader @this, Action<DB2DataReader> forEach)
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
