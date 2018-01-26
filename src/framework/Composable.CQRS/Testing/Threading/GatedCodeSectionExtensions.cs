using System;
using Composable.Contracts;

namespace Composable.Testing.Threading
{
    static class GatedCodeSectionExtensions
    {
        public static IGatedCodeSection Open(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.EntranceGate.Open();
                    @this.ExitGate.Open();
                });

        public static IGatedCodeSection Close(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.EntranceGate.Close();
                    @this.ExitGate.Close();
                });

        public static IGatedCodeSection LetOneThreadEnter(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.AssertIsEmpty();
                    @this.EntranceGate.AwaitLetOneThreadPassthrough();
                });

        public static IGatedCodeSection LetOneThreadEnterAndReachExit(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.LetOneThreadEnter();
                    @this.ExitGate.AwaitQueueLengthEqualTo(1);
                });

        public static IGatedCodeSection LetOneThreadPass(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.LetOneThreadEnterAndReachExit();
                    @this.ExitGate.AwaitLetOneThreadPassthrough();
                });

        public static void Execute(this IGatedCodeSection @this, Action action)
        {
            using(@this.Enter())
            {
                action();
            }
        }

        public static bool IsEmpty(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(() => @this.EntranceGate.Passed == @this.ExitGate.Passed);

        public static IGatedCodeSection AssertIsEmpty(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(() => Contract.Assert.That(@this.IsEmpty(), "Code section should be empty"));

        public static TResult WithExclusiveLock<TResult>(this IGatedCodeSection @this, Func<TResult> function)
        {
            var result = default(TResult);
            @this.WithExclusiveLock(() => result = function());
            return result;
        }
    }
}
