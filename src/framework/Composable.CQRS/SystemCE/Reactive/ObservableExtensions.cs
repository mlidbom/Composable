using System;

namespace Composable.SystemCE.Reactive
{
    static class ObservableExtensions
    {
        public static IDisposable Subscribe<TEvent>(this IObservable<TEvent> @this, Action<TEvent> onNext) => @this.Subscribe( new SimpleObserver<TEvent>(onNext: onNext));
    }
}