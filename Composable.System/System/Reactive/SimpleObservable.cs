using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Composable.System.Linq;

namespace Composable.System.Reactive
{
    ///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
    public class SimpleObservable<TEvent> : IObservable<TEvent>
    {
        private readonly HashSet<IObserver<TEvent>> _observerCollection = new HashSet<IObserver<TEvent>>();

        ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            Contract.Requires(@event != null);
            _observerCollection.ForEach(objserver => objserver.OnNext(@event));
        }

        ///<summary>Implements <see cref="IObservable{T}.Subscribe"/></summary>
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new DisposeAction(() => _observerCollection.Remove(observer));
        }
    }
}