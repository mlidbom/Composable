using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.System.Reactive
{
    public class ThreadSafeObserverCollection<TEvent>
    {
        readonly HashSet<IObserver<TEvent>>  _observers = new HashSet<IObserver<TEvent>>();
        private readonly object _lockObject = new object();

        public void Add(IObserver<TEvent> observer)
        {
            lock(_lockObject)
            {
                _observers.Add(observer);
            }
        }

        public void Remove(IObserver<TEvent> observer)
        {
            lock(_lockObject)
            {
                _observers.Remove(observer);   
            }            
        }

        public void Notify(TEvent @event)
        {
            IObserver<TEvent>[] observers;
            lock (_lockObject)
            {
                observers = _observers.ToArray();
            }
            observers.ForEach(observer => observer.OnNext(@event));
        }
    }
}