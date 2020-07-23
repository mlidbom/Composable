using System;
using System.Transactions;

namespace Composable.SystemCE.TransactionsCE
{
    static class IsolationLevelCE
    {
        public static global::System.Data.IsolationLevel ToDataIsolationLevel(this IsolationLevel @this) => @this switch
        {
            IsolationLevel.Chaos => global::System.Data.IsolationLevel.Chaos,
            IsolationLevel.ReadCommitted => global::System.Data.IsolationLevel.ReadCommitted,
            IsolationLevel.ReadUncommitted => global::System.Data.IsolationLevel.ReadUncommitted,
            IsolationLevel.RepeatableRead => global::System.Data.IsolationLevel.RepeatableRead,
            IsolationLevel.Serializable => global::System.Data.IsolationLevel.Serializable,
            IsolationLevel.Snapshot => global::System.Data.IsolationLevel.Snapshot,
            IsolationLevel.Unspecified => global::System.Data.IsolationLevel.Unspecified,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
