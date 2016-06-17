using System;

namespace Composable.System.Reactive
{
    ///<summary>Suplies a simple and thread safe implementation of IObservable.</summary>
    public class ThreadSafeObservable<TEvent> : IObservable<TEvent>
    {
        private readonly ThreadSafeObserverCollection<TEvent> _observerCollection = new ThreadSafeObserverCollection<TEvent>();

        ///<summary>Invoke <see cref="IObserver{T}.OnNext"/> on each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            _observerCollection.OnNext(@event);
        }

        ///<summary>Implements <see cref="IObservable{T}.Subscribe"/></summary>
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new Disposable(() => _observerCollection.Remove(observer));
        }
    }
}