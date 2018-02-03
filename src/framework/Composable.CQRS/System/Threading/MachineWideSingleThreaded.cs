using System;
using System.Threading;
using JetBrains.Annotations;

namespace Composable.System.Threading
{
    class MachineWideSingleThreaded
    {
        readonly string _lockId;
        MachineWideSingleThreaded(string lockId) => _lockId = $@"Global\{lockId}";

        internal void Execute([InstantHandle]Action action)
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

        internal TResult Execute<TResult>([InstantHandle]Func<TResult> func)
        {
            using(var mutex = new Mutex(initiallyOwned: false, name: _lockId))
            {
                try
                {
                    mutex.WaitOne();
                    return func();
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
