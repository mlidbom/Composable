using System;
using System.Data.Common;

namespace Composable.Persistence.Common.AdoCE
{
    static class DbDataReaderCE
    {
        //Urgent: In all persistence layers, replace all manual implementation of this.
        public static Guid GetGuidFromString(this DbDataReader @this, int index) => Guid.Parse(@this.GetString(index));
        public static void ForEachSuccessfulRead<TReader>(this TReader @this, Action<TReader> forEach) where TReader : DbDataReader
        {
            while(@this.Read()) forEach(@this);
        }
    }
}
