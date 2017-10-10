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
        IResourceGuard _guard;
        TResource _resource;

        public static GuardedResource<TResource> WithTimeout(TimeSpan timeout) => new GuardedResource<TResource>(ResourceGuard.WithTimeout(timeout), new TResource());

        GuardedResource(IResourceGuard guard, TResource resource)
        {
            _guard = guard;
            _resource = resource;
        }

        public TResult Locked<TResult>(Func<TResource, TResult> func) => _guard.Update(() => func(_resource));
        public void Locked(Action<TResource> func) => _guard.Update(() => func(_resource));
    }
}
