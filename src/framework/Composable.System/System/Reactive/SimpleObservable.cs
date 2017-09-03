using System;
using System.Collections.Generic;

using Composable.Contracts;
using Composable.System.Linq;

namespace Composable.System.Reactive
{
    ///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
    class SimpleObservable<TEvent> : IObservable<TEvent>
    {
        readonly HashSet<IObserver<TEvent>> _observerCollection = new HashSet<IObserver<TEvent>>();

        ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            ContractOptimized.Argument(@event, nameof(@event)).NotNull();

            _observerCollection.ForEach(objserver => objserver.OnNext(@event));
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new Disposable(() => _observerCollection.Remove(observer));
        }
    }
}