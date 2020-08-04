using System;
using Composable.SystemCE.ReflectionCE;

namespace Composable.SystemCE.ThreadingCE.ResourceAccess
{
    interface IThreadShared<out TResource>
    {
        TResult Read<TResult>(Func<TResource, TResult> read);
        TResult Update<TResult>(Func<TResource, TResult> update);
        void Update(Action<TResource> update);
        void Await(Func<TResource, bool> condition);
        void UpdateWhen(Func<TResource, bool> condition, Action<TResource> update);
        TResult UpdateWhen<TResult>(Func<TResource, bool> condition, Func<TResource, TResult> update);
    }

    static class ThreadShared
    {
        public static IThreadShared<TShared> Create<TShared>() where TShared : new() =>
            new ResourceGuardThreadShared<TShared>(Constructor.For<TShared>.DefaultConstructor.Instance());

        public static IThreadShared<TShared> WithTimeout<TShared>(TimeSpan timeout) where TShared : new() =>
            new ResourceGuardThreadShared<TShared>(timeout, Constructor.For<TShared>.DefaultConstructor.Instance());

        public static IThreadShared<TShared> Create<TShared>(TShared shared) =>
            new ResourceGuardThreadShared<TShared>(shared);

        public static IThreadShared<TShared> WithTimeout<TShared>(TimeSpan timeOut, TShared shared) =>
            new ResourceGuardThreadShared<TShared>(shared);


        class ResourceGuardThreadShared<TShared> : IThreadShared<TShared>
        {
            readonly IResourceGuard _guard;

            readonly TShared _shared;

            internal ResourceGuardThreadShared(TimeSpan timeout, TShared shared)
            {
                _shared = shared;
                _guard = ResourceGuard.WithTimeout(timeout);
            }

            internal ResourceGuardThreadShared(TShared shared)
            {
                _shared = shared;
                _guard = ResourceGuard.Create();
            }

            public TResult Read<TResult>(Func<TShared, TResult> read) =>
                _guard.Read(() => read(_shared));

            public TResult Update<TResult>(Func<TShared, TResult> update) =>
                _guard.Update(() => update(_shared));

            public void Update(Action<TShared> update) =>
                _guard.Update(() => update(_shared));

            public void Await(Func<TShared, bool> condition) =>
                _guard.Await(() => condition(_shared));

            public void UpdateWhen(Func<TShared, bool> condition, Action<TShared> update) =>
                _guard.UpdateWhen(() => condition(_shared), () => update(_shared));

            public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update) =>
                _guard.UpdateWhen(() => condition(_shared), () => update(_shared));
        }
    }
}
