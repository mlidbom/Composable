using System;

namespace Composable.System.Reactive
{
    class SimpleObserver<TEvent> : IObserver<TEvent>
    {
        readonly Action<TEvent> _onNext;
        readonly Action<Exception> _onError;
        readonly Action _onCompleted;

        public SimpleObserver(Action<TEvent> onNext = null, Action<Exception> onError = null, Action onCompleted = null)
        {
            _onNext = onNext ?? (ignored => { });
            _onError = onError ?? (ignored => { });
            _onCompleted = onCompleted ?? (() => { });
        }

        public void OnNext(TEvent value) => _onNext(value);
        public void OnError(Exception error) => _onError(error);
        public void OnCompleted() => _onCompleted();
    }

    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<TEvent>(this IObservable<TEvent> @this, Action<TEvent> onNext)
        {
            return @this.Subscribe( new SimpleObserver<TEvent>(onNext: onNext));
        }
    }
}