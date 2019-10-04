using System;
using System.Threading;
using JetBrains.Annotations;

namespace Composable.System.Threading.ResourceAccess
{
    interface IThreadShared<out TResource>
    {
        TResult WithExclusiveAccess<TResult>(Func<TResource, TResult> func);
        void WithExclusiveAccess([InstantHandle]Action<TResource> func);
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

    class OptimizedThreadShared<TResource> : IThreadShared<TResource>
    {
        readonly TResource _resource;
        readonly object _lock = new object();
        public OptimizedThreadShared(TResource resource) => _resource = resource;

        public TResult ByPass<TResult>(Func<TResource, TResult> func) => func(_resource);

        public void ByPass(Action<TResource> func) => func(_resource);

        public TResult WithExclusiveAccess<TResult>(Func<TResource, TResult> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));

        public void WithExclusiveAccess(Action<TResource> func) => ResourceGuard.WithExclusiveLock(_lock, () => func(_resource));
    }

    class AwaitableOptimizedThreadShared<TShared>
    {
        readonly object _lock = new object();
        readonly TShared _shared;
        public AwaitableOptimizedThreadShared(TShared shared) => _shared = shared;

        public TResult Read<TResult>(Func<TShared, TResult> read)
        {
            lock(_lock)
            {
                return read(_shared);
            }
        }

        public TResult Update<TResult>(Func<TShared, TResult> update)
        {
            lock(_lock)
            {
                var result = update(_shared);
                Monitor.PulseAll(_lock);
                return result;
            }
        }

        public void Update(Action<TShared> update)
        {
            lock(_lock)
            {
                update(_shared);
                Monitor.PulseAll(_lock);
            }
        }

        public void Await(Func<TShared, bool> condition)
        {
            lock(_lock)
            {
                while(!condition(_shared))
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        public void UpdateWhen(Func<TShared, bool> condition, Action<TShared> update)
        {
            lock(_lock)
            {
                while(!condition(_shared))
                {
                    Monitor.Wait(_lock);
                }

                update(_shared);
                Monitor.PulseAll(_lock);
            }
        }

        public TResult UpdateWhen<TResult>(Func<TShared, bool> condition, Func<TShared, TResult> update)
        {
            lock(_lock)
            {
                while(!condition(_shared))
                {
                    Monitor.Wait(_lock);
                }

                var result = update(_shared);
                Monitor.PulseAll(_lock);
                return result;
            }
        }
    }
}
