using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Hypermedia
{
    public abstract class NavigationSpecification
    {
        public static NavigationSpecification Post(MessageTypes.Remotable.AtMostOnce.ICommand command) => new VoidCommand(command);

        public static NavigationSpecification<TResult> Get<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => NavigationSpecification<TResult>.Get(query);
        public static NavigationSpecification<TResult> Post<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => NavigationSpecification<TResult>.Post(command);

        public void NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
        public abstract Task NavigateOnAsync(IRemoteHypermediaNavigator busSession);

        class VoidCommand : NavigationSpecification
        {
            readonly MessageTypes.Remotable.AtMostOnce.ICommand _command;

            public VoidCommand(MessageTypes.Remotable.AtMostOnce.ICommand command) => _command = command;

            public override async Task NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_command).NoMarshalling();
        }
    }

    public abstract class NavigationSpecification<TResult>
    {
        public TResult NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
        public abstract Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession);

        public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public NavigationSpecification Post(Func<TResult, MessageTypes.Remotable.AtMostOnce.ICommand> next) => new PostVoidCommand<TResult>(this, next);
        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, MessageTypes.Remotable.NonTransactional.IQuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, MessageTypes.Remotable.AtMostOnce.ICommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);

        internal static NavigationSpecification<TResult> Get(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => new StartQuery(query);
        internal static NavigationSpecification<TResult> Post(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => new StartCommand(command);

        class SelectQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, TResult> _select;

            internal SelectQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
            {
                _previous = previous;
                _select = select;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession).NoMarshalling();
                return _select(previousResult);
            }
        }

        class StartQuery : NavigationSpecification<TResult>
        {
            readonly MessageTypes.Remotable.NonTransactional.IQuery<TResult> _start;

            internal StartQuery(MessageTypes.Remotable.NonTransactional.IQuery<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.GetAsync(_start).NoMarshalling();
        }

        class StartCommand : NavigationSpecification<TResult>
        {
            readonly MessageTypes.Remotable.AtMostOnce.ICommand<TResult> _start;

            internal StartCommand(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_start).NoMarshalling();
        }

        class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, MessageTypes.Remotable.NonTransactional.IQuery<TResult>> _nextQuery;

            internal ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, MessageTypes.Remotable.NonTransactional.IQuery<TResult>> nextQuery)
            {
                _previous = previous;
                _nextQuery = nextQuery;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession).NoMarshalling();
                var currentQuery = _nextQuery(previousResult);
                return await busSession.GetAsync(currentQuery).NoMarshalling();
            }
        }

        class PostCommand<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, MessageTypes.Remotable.AtMostOnce.ICommand<TResult>> _next;
            internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, MessageTypes.Remotable.AtMostOnce.ICommand<TResult>> next)
            {
                _previous = previous;
                _next = next;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession).NoMarshalling();
                var currentCommand = _next(previousResult);
                return await busSession.PostAsync(currentCommand).NoMarshalling();
            }
        }

        class PostVoidCommand<TPrevious> : NavigationSpecification
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, MessageTypes.Remotable.AtMostOnce.ICommand> _next;
            internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, MessageTypes.Remotable.AtMostOnce.ICommand> next)
            {
                _previous = previous;
                _next = next;
            }

            public override async Task NavigateOnAsync(IRemoteHypermediaNavigator busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession).NoMarshalling();
                var currentCommand = _next(previousResult);
                await busSession.PostAsync(currentCommand).NoMarshalling();
            }
        }
    }
}
