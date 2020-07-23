using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Composable.SystemCE.CollectionsCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.SystemCE.ThreadingCE
{
    class MachineWideSingleThreaded
    {
        static readonly OptimizedThreadShared<Dictionary<string, Mutex>> Cache = new OptimizedThreadShared<Dictionary<string, Mutex>>(new Dictionary<string, Mutex>());

        readonly Mutex _mutex;
        MachineWideSingleThreaded(string lockId)
        {
            var lockId1 = $@"Global\{lockId}";

            _mutex = Cache.WithExclusiveAccess(cache => cache.GetOrAdd(lockId1,
                                                                       () =>
                                                                       {
                                                                           try
                                                                           {
                                                                               var existing = Mutex.OpenExisting(lockId1);
                                                                               return existing;
                                                                           }
                                                                           catch
                                                                           {
                                                                               var mutex = new Mutex(initiallyOwned: false, name: lockId1);

                                                                               MutexSecurity mutexSecurity = new MutexSecurity();
                                                                               mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                                                                                                                               MutexRights.FullControl,
                                                                                                                               AccessControlType.Allow));
                                                                               mutex.SetAccessControl(mutexSecurity);

                                                                               return mutex;
                                                                           }
                                                                       }));
        }

        internal void Execute([InstantHandle] Action action)
        {
            try
            {
                _mutex.WaitOne();
                action();
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        internal TResult Execute<TResult>([InstantHandle] Func<TResult> func)
        {
            try
            {
                _mutex.WaitOne();
                return func();
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        internal static MachineWideSingleThreaded For(string name) => new MachineWideSingleThreaded(name);
        internal static MachineWideSingleThreaded For<TSynchronized>() => For(typeof(TSynchronized));
        internal static MachineWideSingleThreaded For(Type synchronized) => new MachineWideSingleThreaded($"{nameof(MachineWideSingleThreaded)}_{synchronized.AssemblyQualifiedName}");
    }
}
