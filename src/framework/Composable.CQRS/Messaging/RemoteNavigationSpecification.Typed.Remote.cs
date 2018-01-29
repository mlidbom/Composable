using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class RemoteNavigationSpecification<TResult>
    {
        internal static class Remote
        {
            internal class StartQuery : RemoteNavigationSpecification<TResult>
            {
                readonly BusApi.RemoteSupport.NonTransactional.IQuery<TResult> _start;

                internal StartQuery(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> start) => _start = start;

                public override TResult NavigateOn(IRemoteApiBrowser busSession) => busSession.Get(_start);
                public override Task<TResult> NavigateOnAsync(IRemoteApiBrowser busSession) => busSession.GetAsync(_start);
            }

            internal class StartCommand : RemoteNavigationSpecification<TResult>
            {
                readonly BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> _start;

                internal StartCommand(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> start) => _start = start;

                public override TResult NavigateOn(IRemoteApiBrowser busSession) => busSession.Post(_start);
                public override Task<TResult> NavigateOnAsync(IRemoteApiBrowser busSession) => busSession.PostAsync(_start);
            }

            internal class ContinuationQuery<TPrevious> : RemoteNavigationSpecification<TResult>
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.RemoteSupport.NonTransactional.IQuery<TResult>> _nextQuery;

                internal ContinuationQuery(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.NonTransactional.IQuery<TResult>> nextQuery)
                {
                    _previous = previous;
                    _nextQuery = nextQuery;
                }

                public override TResult NavigateOn(IRemoteApiBrowser busSession)
                {
                    var previousResult = _previous.NavigateOn(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return busSession.Get(currentQuery);
                }

                public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowser busSession)
                {
                    var previousResult = await _previous.NavigateOnAsync(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return await busSession.GetAsync(currentQuery);
                }
            }

            internal class PostCommand<TPrevious> : RemoteNavigationSpecification<TResult>
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand<TResult>> _next;
                internal PostCommand(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand<TResult>> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override TResult NavigateOn(IRemoteApiBrowser busSession)
                {
                    var previousResult = _previous.NavigateOn(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.Post(currentCommand);
                }

                public override async Task<TResult> NavigateOnAsync(IRemoteApiBrowser busSession)
                {
                    var previousResult = await _previous.NavigateOnAsync(busSession);
                    var currentCommand = _next(previousResult);
                    return await busSession.PostAsync(currentCommand);
                }
            }

            internal class PostVoidCommand<TPrevious> : RemoteNavigationSpecification
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand> _next;
                internal PostVoidCommand(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.RemoteSupport.AtMostOnce.ICommand> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override void NavigateOn(IRemoteApiBrowser busSession)
                {
                    var previousResult = _previous.NavigateOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.Post(currentCommand);
                }

                public override async Task NavigateOnAsync(IRemoteApiBrowser busSession)
                {
                    var previousResult = await _previous.NavigateOnAsync(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.Post(currentCommand);
                }
            }
        }
    }
}
