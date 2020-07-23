using System;
using System.Collections.Generic;
using Composable.Contracts;
using Composable.SystemCE.LinqCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE.Reactive
{
    ///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
    class SimpleObservable<TEvent> : IObservable<TEvent>
    {
        readonly IThreadShared<HashSet<IObserver<TEvent>>> _observerCollection = ThreadShared<HashSet<IObserver<TEvent>>>.Optimized();

        ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            Contract.ArgumentNotNull(@event, nameof(@event));

            _observerCollection.WithExclusiveAccess(@this => @this.ForEach(observer => observer.OnNext(@event)));
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.WithExclusiveAccess(@this =>  @this.Add(observer));
            return DisposableCE.Create(() => _observerCollection.WithExclusiveAccess(@this => @this.Remove(observer)));
        }
    }
}