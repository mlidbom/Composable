using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;
using Composable.System.Threading;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public static NavigationSpecification Post(BusApi.RemoteSupport.AtMostOnce.ICommand command) => new VoidCommand(command);

        public static NavigationSpecification<TResult> Get<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => NavigationSpecification<TResult>.Get(query);
        public static NavigationSpecification<TResult> Post<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => NavigationSpecification<TResult>.Post(command);

        public void NavigateOn(IRemoteApiBrowserSession busSession) => NavigateOnAsync(busSession).WaitUnwrappingException();
        public abstract Task NavigateOnAsync(IRemoteApiBrowserSession busSession);

        class VoidCommand : NavigationSpecification
        {
            readonly BusApi.RemoteSupport.AtMostOnce.ICommand _command;

            public VoidCommand(BusApi.RemoteSupport.AtMostOnce.ICommand command) => _command = command;

            public override async Task NavigateOnAsync(IRemoteApiBrowserSession busSession) => await busSession.PostAsync(_command);
        }
    }

    public abstract class NavigationSpecification<TResult>
    {
        public TResult NavigateOn(IRemoteApiBrowserSession busSession) => NavigateOnAsync(busSession).ResultUnwrappingException();
        public abstract Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession);

        public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public NavigationSpecification Post(Func<TResult, BusApi.RemoteSupport.AtMostOnce.ICommand> next) => new PostVoidCommand<TResult>(this, next);
        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, BusApi.RemoteSupport.NonTransactional.IQuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, BusApi.RemoteSupport.AtMostOnce.ICommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);

        internal static NavigationSpecification<TResult> Get(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => new StartQuery(query);
        internal static NavigationSpecification<TResult> Post(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => new StartCommand(command);

        class SelectQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, TResult> _select;

            internal SelectQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
            {
                _previous = previous;
                _select = select;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession);
                return _select(previousResult);
            }
        }

        class StartQuery : NavigationSpecification<TResult>
        {
            readonly BusApi.RemoteSupport.NonTransactional.IQuery<TResult> _start;

            internal StartQuery(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession) => await busSession.GetAsync(_start);
        }

        class StartCommand : NavigationSpecification<TResult>
        {
            readonly BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> _start;

            internal StartCommand(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> start) => _start = start;

            public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession) => await busSession.PostAsync(_start);
        }

        class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, BusApi.RemoteSupport.NonTransactional.IQuery<TResult>> _nextQuery;

            internal ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.NonTransactional.IQuery<TResult>> nextQuery)
            {
                _previous = previous;
                _nextQuery = nextQuery;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession);
                var currentQuery = _nextQuery(previousResult);
                return await busSession.GetAsync(currentQuery);
            }
        }

        class PostCommand<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand<TResult>> _next;
            internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand<TResult>> next)
            {
                _previous = previous;
                _next = next;
            }

            public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowserSession busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession);
                var currentCommand = _next(previousResult);
                return await busSession.PostAsync(currentCommand);
            }
        }

        class PostVoidCommand<TPrevious> : NavigationSpecification
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand> _next;
            internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand> next)
            {
                _previous = previous;
                _next = next;
            }

            public override async Task NavigateOnAsync(IRemoteApiBrowserSession busSession)
            {
                var previousResult = await _previous.NavigateOnAsync(busSession);
                var currentCommand = _next(previousResult);
                await busSession.PostAsync(currentCommand);
            }
        }
    }
}
