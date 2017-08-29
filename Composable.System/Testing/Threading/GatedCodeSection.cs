using System;
using Composable.Contracts;
using Composable.System;
using Composable.System.Threading;

namespace Composable.Testing.Threading
{
    interface IGatedCodeSection
    {
        IThreadGate EntranceGate { get; }
        IThreadGate ExitGate { get; }
        IGatedCodeSection WithExclusiveLock(Action action);
        IDisposable Enter();
    }

    static class GatedCodeSectionExtensions
    {
        public static TResult WithExclusiveLock<TResult>(this IGatedCodeSection @this, Func<TResult> function)
        {
            var result = default(TResult);
            @this.WithExclusiveLock(() => result = function());
            return result;
        }

        public static IGatedCodeSection LetOneThreadEnter(this IGatedCodeSection @this)
        {
            @this.EntranceGate.LetOneThreadPass();
            return @this;
        }

        public static IGatedCodeSection LetOneThreadEnterAndReachExit(this IGatedCodeSection @this)
        {
            return @this.WithExclusiveLock(() =>
                                           {
                                               @this.AssertIsEmpty();
                                               @this.EntranceGate.LetOneThreadPass();
                                               @this.ExitGate.AwaitQueueLength(1);
                                           });
        }

        public static bool IsEmpty(this IGatedCodeSection @this) => @this.WithExclusiveLock(() => @this.EntranceGate.Passed == @this.ExitGate.Passed);

        public static IGatedCodeSection AssertIsEmpty(this IGatedCodeSection @this)
        {
            Contract.Assert.That(@this.IsEmpty(), "Code section should be empty");
            return @this;
        }

        public static IGatedCodeSection Open(this IGatedCodeSection @this)
        {
            @this.EntranceGate.Open();
            @this.ExitGate.Open();
            return @this;
        }

        public static IGatedCodeSection LetOneThreadPass(this IGatedCodeSection @this)
        {
            @this.EntranceGate.LetOneThreadPass();
            @this.ExitGate.LetOneThreadPass();
            return @this;
        }
    }

    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    class GatedCodeSection : IGatedCodeSection
    {
        IObjectLock _lock;
        public IThreadGate EntranceGate { get; }
        public IThreadGate ExitGate { get; }

        public static IGatedCodeSection WithTimeout(TimeSpan timeout) => new GatedCodeSection(timeout);

        GatedCodeSection(TimeSpan timeout)
        {
            _lock = ObjectLock.WithTimeout(timeout);
            EntranceGate = ThreadGate.WithTimeout(timeout);
            ExitGate = ThreadGate.WithTimeout(timeout);
        }

        public IGatedCodeSection WithExclusiveLock(Action action)
        {
            using (_lock.LockForExclusiveUse())
            {
                action();
            }
            return this;
        }

        public IDisposable Enter()
        {
            EntranceGate.Pass();
            return Disposable.Create(() => ExitGate.Pass());
        }
    }
}
