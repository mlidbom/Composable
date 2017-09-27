using System.Threading;
using JetBrains.Annotations;

namespace Composable.Testing.Threading
{
    class ThreadSnapshot
    {
        public Thread Thread { get; } = Thread.CurrentThread;

        [CanBeNull] public TransactionSnapshot Transaction { get; } = TransactionSnapshot.TakeSnapshot();
    }
}
