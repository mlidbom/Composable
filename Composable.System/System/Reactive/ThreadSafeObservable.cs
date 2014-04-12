using System;

namespace Composable.System.Reactive
{
    public class ThreadSafeObservable<TEvent> : IObservable<TEvent>
    {
        private readonly ThreadSafeObserverCollection<TEvent> _observerCollection = new ThreadSafeObserverCollection<TEvent>();

        public void OnNext(TEvent @event)
        {
            _observerCollection.Notify(@event);
        }

        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new DisposeAction(() => _observerCollection.Remove(observer));
        }
    }
}