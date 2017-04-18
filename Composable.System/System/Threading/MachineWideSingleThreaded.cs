using System;
using System.Threading;

namespace Composable.System.Threading
{
    class MachineWideSingleThreaded
    {
        readonly string _lockId;
        MachineWideSingleThreaded(string lockId) => _lockId = lockId;

        internal void Execute(Action action)
        {
            using(var mutex = new Mutex(initiallyOwned: false, name: _lockId))
            {
                try
                {
                    mutex.WaitOne();
                    action();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        internal static MachineWideSingleThreaded For(string name) => new MachineWideSingleThreaded(name);
        internal static MachineWideSingleThreaded For<TSynchronized>() => For(typeof(TSynchronized));
        internal static MachineWideSingleThreaded For(Type synchronized) => new MachineWideSingleThreaded($"{nameof(MachineWideSingleThreaded)}_{synchronized.AssemblyQualifiedName}");
    }
}
