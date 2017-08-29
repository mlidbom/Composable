using System;
using System.Collections.Generic;
using System.Threading;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    interface IThreadGateVisitor
    {
        void Pass();
        void Pass(TimeSpan timeout);
    }

    interface IThreadGate : IThreadGateVisitor
    {
        ///<summary>Opens the gate and lets all threads through.</summary>
        IThreadGate Open();

        ///<summary>Lets a single thread pass.</summary>
        IThreadGate LetOneThreadPass();

        ///<summary>Blocks all threads from passing.</summary>
        IThreadGate Close();

        ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
        IThreadGate ExecuteLockedOnce(TimeSpan timeout, Predicate<IThreadGate> condition, Action<IThreadGate, IExclusiveResourceLock> action);

        bool IsOpen { get; }
        long Queued { get; }
        long Requested { get; }
        long Passed { get; }
        TimeSpan DefaultTimeout { get; }

        IReadOnlyList<Thread> RequestedThreads { get; }
        IReadOnlyList<Thread> QueuedThreads { get; }
        IReadOnlyList<Thread> PassedThreads { get; }
    }

    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    interface IGatedCodeSection
    {
        IThreadGate EntranceGate { get; }
        IThreadGate ExitGate { get; }
        IGatedCodeSection WithExclusiveLock(Action action);
        IDisposable Enter();
    }
}
