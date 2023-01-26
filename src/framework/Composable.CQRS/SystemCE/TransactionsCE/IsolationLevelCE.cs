using System;
using System.Transactions;

namespace Composable.SystemCE.TransactionsCE
{
    static class IsolationLevelCE
    {
        public static System.Data.IsolationLevel ToDataIsolationLevel(this IsolationLevel @this) => @this switch
        {
            IsolationLevel.Chaos => System.Data.IsolationLevel.Chaos,
            IsolationLevel.ReadCommitted => System.Data.IsolationLevel.ReadCommitted,
            IsolationLevel.ReadUncommitted => System.Data.IsolationLevel.ReadUncommitted,
            IsolationLevel.RepeatableRead => System.Data.IsolationLevel.RepeatableRead,
            IsolationLevel.Serializable => System.Data.IsolationLevel.Serializable,
            IsolationLevel.Snapshot => System.Data.IsolationLevel.Snapshot,
            IsolationLevel.Unspecified => System.Data.IsolationLevel.Unspecified,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
