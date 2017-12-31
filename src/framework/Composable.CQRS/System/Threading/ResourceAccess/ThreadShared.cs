using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IThreadShared<out TResource>
    {
        TResult WithExclusiveAccess<TResult>(Func<TResource, TResult> func);
        void WithExclusiveAccess(Action<TResource> func);
    }

    class ThreadShared<TResource> : IThreadShared<TResource> where TResource : new()
    {
        readonly IResourceGuard _guard;
        readonly TResource _resource;

        public static ThreadShared<TResource> WithTimeout(TimeSpan timeout) => new ThreadShared<TResource>(ResourceGuard.WithTimeout(timeout), new TResource());
        public static IThreadShared<TResource> Optimized() => new OptimizedThreadShared<TResource>(new TResource());

        ThreadShared(IResourceGuard guard, TResource resource)
        {
            _guard = guard;
            _resource = resource;
        }

        public TResult WithExclusiveAccess<TResult>(Func<TResource, TResult> func) => _guard.Update(() => func(_resource));
        public void WithExclusiveAccess(Action<TResource> func) => _guard.Update(() => func(_resource));
    }

    class OptimizedThreadShared<TResource> : IThreadShared<TResource> where TResource : new()
    {
        readonly TResource _resource;
        readonly object _lock = new object();
        public OptimizedThreadShared(TResource resource) => _resource = resource;

        public TResult WithExclusiveAccess<TResult>(Func<TResource, TResult> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));

        public void WithExclusiveAccess(Action<TResource> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));
    }
}
