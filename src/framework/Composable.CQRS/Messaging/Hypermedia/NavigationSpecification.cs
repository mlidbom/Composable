using System;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;

namespace Composable.Messaging.Hypermedia
{
    public abstract class NavigationSpecification
    {
        public static NavigationSpecification Post(IAtMostOnceHypermediaCommand command) => new VoidCommand(command);

        public static NavigationSpecification<TResult> Get<TResult>(IRemotableQuery<TResult> query) => NavigationSpecification<TResult>.Get(query);
        public static NavigationSpecification<TResult> Post<TResult>(IAtMostOnceCommand<TResult> command) => NavigationSpecification<TResult>.Post(command);

        public void NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
        public abstract Task NavigateOnAsync(IRemoteHypermediaNavigator busSession);

        class VoidCommand : NavigationSpecification
        {
            readonly IAtMostOnceHypermediaCommand _command;

            public VoidCommand(IAtMostOnceHypermediaCommand command) => _command = command;

            public override async Task NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_command).NoMarshalling();
        }
    }

    public abstract class NavigationSpecification<TResult>
    {
        public TResult NavigateOn(IRemoteHypermediaNavigator busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
        public abstract Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession);

        public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public NavigationSpecification Post(Func<TResult, IAtMostOnceHypermediaCommand> next) => new PostVoidCommand<TResult>(this, next);
        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IRemotableQuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, IAtMostOnceCommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);

        internal static NavigationSpecification<TResult> Get(IRemotableQuery<TResult> query) => new StartQuery(query);
        internal static NavigationSpecification<TResult> Post(IAtMostOnceCommand<TResult> command) => new StartCommand(command);

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
            readonly IRemotableQuery<TResult> _start;

            internal StartQuery(IRemotableQuery<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.GetAsync(_start).NoMarshalling();
        }

        class StartCommand : NavigationSpecification<TResult>
        {
            readonly IAtMostOnceCommand<TResult> _start;

            internal StartCommand(IAtMostOnceCommand<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteHypermediaNavigator busSession) => await busSession.PostAsync(_start).NoMarshalling();
        }

        class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, IRemotableQuery<TResult>> _nextQuery;

            internal ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IRemotableQuery<TResult>> nextQuery)
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
            readonly Func<TPrevious, IAtMostOnceCommand<TResult>> _next;
            internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceCommand<TResult>> next)
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
            readonly Func<TPrevious, IAtMostOnceHypermediaCommand> _next;
            internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IAtMostOnceHypermediaCommand> next)
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
