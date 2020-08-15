﻿using System;
using System.Collections.Generic;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.Testing.Threading
{
    interface IThreadGateVisitor
    {
        void AwaitPassThrough();
    }

    interface IThreadGate : IThreadGateVisitor
    {
        ///<summary>Opens the gate and lets all threads through.</summary>
        IThreadGate Open();

        ///<summary>Lets a single thread pass.</summary>
        IThreadGate AwaitLetOneThreadPassThrough();

        ///<summary>Blocks all threads from passing.</summary>
        IThreadGate Close();

        IThreadGate SetPrePassThroughAction(Action<ThreadSnapshot> action);
        IThreadGate SetPostPassThroughAction(Action<ThreadSnapshot> action);
        IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action);

        Action<ThreadSnapshot> PassThroughAction { get; }

        ///<summary>Blocks until the gate is in a state which satisfies <see cref="condition"/> and then while owning the lock executes <see cref="action"/></summary>
        IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action action);

        bool TryAwait(TimeSpan timeout, Func<bool> condition);

        bool IsOpen { get; }
        long Queued { get; }
        long Requested { get; }
        long Passed { get; }
        TimeSpan DefaultTimeout { get; }

        IReadOnlyList<ThreadSnapshot> RequestedThreads { get; }
        IReadOnlyList<ThreadSnapshot> QueuedThreads { get; }
        IReadOnlyList<ThreadSnapshot> PassedThrough { get; }
    }

    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    interface IGatedCodeSection
    {
        IThreadGate EntranceGate { get; }
        IThreadGate ExitGate { get; }
        IDisposable Enter();
    }
}
