using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.System.Reactive
{
    ///<summary>A thread safe collection of <see cref="IObserver{T}"/> instances.</summary>
    public class ThreadSafeObserverCollection<TEvent>
    {
        readonly HashSet<IObserver<TEvent>>  _observers = new HashSet<IObserver<TEvent>>();
        private readonly object _lockObject = new object();

        ///<summary>Add an observer to the collection.</summary>
        public void Add(IObserver<TEvent> observer)
        {
            lock(_lockObject)
            {
                _observers.Add(observer);
            }
        }

        ///<summary>Removes an observer from the collection.</summary>
        public void Remove(IObserver<TEvent> observer)
        {
            lock(_lockObject)
            {
                _observers.Remove(observer);   
            }            
        }

        ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each observer in the collection.</summary>
        public void OnNext(TEvent @event)
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