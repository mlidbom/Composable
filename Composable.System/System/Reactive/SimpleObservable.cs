using System;
using System.Collections.Generic;
using Composable.System.Linq;

namespace Composable.System.Reactive
{
    public class SimpleObservable<TEvent> : IObservable<TEvent>
    {
        private readonly HashSet<IObserver<TEvent>> _observerCollection = new HashSet<IObserver<TEvent>>();

        public void OnNext(TEvent @event)
        {
            _observerCollection.ForEach(objserver => objserver.OnNext(@event));
        }

        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new DisposeAction(() => _observerCollection.Remove(observer));
        }
    }
}