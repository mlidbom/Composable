using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IGuardedResource<TResource>
    {
        TResult Locked<TResult>(Func<TResource, TResult> func);
        void Locked(Action<TResource> func);
    }

    class GuardedResource<TResource> : IGuardedResource<TResource> where TResource : new()
    {
        readonly IResourceGuard _guard;
        readonly TResource _resource;

        public static GuardedResource<TResource> WithTimeout(TimeSpan timeout) => new GuardedResource<TResource>(ResourceGuard.WithTimeout(timeout), new TResource());
        public static IGuardedResource<TResource> Optimized() => new OptimizedGuardedResource<TResource>(new TResource());

        GuardedResource(IResourceGuard guard, TResource resource)
        {
            _guard = guard;
            _resource = resource;
        }

        public TResult Locked<TResult>(Func<TResource, TResult> func) => _guard.Update(() => func(_resource));
        public void Locked(Action<TResource> func) => _guard.Update(() => func(_resource));
    }

    class OptimizedGuardedResource<TResource> : IGuardedResource<TResource> where TResource : new()
    {
        readonly TResource _resource;
        readonly object _lock = new object();
        public OptimizedGuardedResource(TResource resource) => _resource = resource;

        public TResult Locked<TResult>(Func<TResource, TResult> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));

        public void Locked(Action<TResource> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));
    }
}
