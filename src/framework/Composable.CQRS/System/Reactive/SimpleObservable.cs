using System;
using System.Collections.Generic;

using Composable.Contracts;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;

namespace Composable.System.Reactive
{
    ///<summary>Simple implementation of <see cref="IObservable{T}"/> that tracks subscribers and allows for calling OnNext on them all at once.</summary>
    class SimpleObservable<TEvent> : IObservable<TEvent>
    {
        readonly IGuardedResource<HashSet<IObserver<TEvent>>> _observerCollection = GuardedResource<HashSet<IObserver<TEvent>>>.Optimized();

        ///<summary>Calls <see cref="IObserver{T}.OnNext"/> for each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            ContractOptimized.Argument(@event, nameof(@event)).NotNull();

            _observerCollection.Locked(@this => @this.ForEach(observer => observer.OnNext(@event)));
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Locked(@this =>  @this.Add(observer));
            return Disposable.Create(() => _observerCollection.Locked(@this => @this.Remove(observer)));
        }
    }
}