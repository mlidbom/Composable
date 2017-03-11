using System;

namespace Composable.System.Reactive
{
    static class ObservableExtensions
    {
        public static IDisposable Subscribe<TEvent>(this IObservable<TEvent> @this, Action<TEvent> onNext)
        {
            return @this.Subscribe( new SimpleObserver<TEvent>(onNext: onNext));
        }
    }
}