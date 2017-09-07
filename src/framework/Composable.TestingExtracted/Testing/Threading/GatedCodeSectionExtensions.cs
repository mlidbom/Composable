using System;
using Composable.Testing.Contracts;

namespace Composable.Testing.Testing.Threading
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

        public static IGatedCodeSection LetOneThreadEnter(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.AssertIsEmpty();
                    @this.EntranceGate.LetOneThreadPass();
                });

        public static IGatedCodeSection LetOneThreadEnterAndReachExit(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.LetOneThreadEnter();
                    @this.ExitGate.AwaitQueueLength(1);
                });

        public static IGatedCodeSection LetOneThreadPass(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(
                () =>
                {
                    @this.LetOneThreadEnterAndReachExit();
                    @this.ExitGate.LetOneThreadPass();
                });

        public static bool IsEmpty(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(() => @this.EntranceGate.Passed == @this.ExitGate.Passed);

        public static IGatedCodeSection AssertIsEmpty(this IGatedCodeSection @this)
            => @this.WithExclusiveLock(() => Contract.AssertThat(@this.IsEmpty(), "Code section should be empty"));

        public static TResult WithExclusiveLock<TResult>(this IGatedCodeSection @this, Func<TResult> function)
        {
            var result = default(TResult);
            @this.WithExclusiveLock(() => result = function());
            return result;
        }
    }
}
