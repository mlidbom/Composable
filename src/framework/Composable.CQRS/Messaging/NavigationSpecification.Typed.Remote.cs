using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class NavigationSpecification<TResult>
    {
        internal static class Remote
        {
            internal class StartQuery : NavigationSpecification<TResult>
            {
                readonly IQuery<TResult> _start;

                internal StartQuery(IQuery<TResult> start) => _start = start;

                public override TResult ExecuteOn(IServiceBusSession busSession) => busSession.GetRemote(_start);
                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => busSession.GetRemoteAsync(_start);
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly IExactlyOnceCommand<TResult> _start;

                internal StartCommand(IExactlyOnceCommand<TResult> start) => _start = start;

                public override TResult ExecuteOn(IServiceBusSession busSession) => busSession.PostRemote(_start);
                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => busSession.PostRemoteAsync(_start);
            }

            internal class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
            {
                readonly NavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, IQuery<TResult>> _nextQuery;

                internal ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IQuery<TResult>> nextQuery)
                {
                    _previous = previous;
                    _nextQuery = nextQuery;
                }

                public override TResult ExecuteOn(IServiceBusSession busSession)
                {
                    var previousResult = _previous.ExecuteOn(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return busSession.GetRemote(currentQuery);
                }

                public override async Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsyncOn(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return await busSession.GetRemoteAsync(currentQuery);
                }
            }

            internal class PostCommand<TPrevious> : NavigationSpecification<TResult>
            {
                readonly NavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, IExactlyOnceCommand<TResult>> _next;
                internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IExactlyOnceCommand<TResult>> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override TResult ExecuteOn(IServiceBusSession busSession)
                {
                    var previousResult = _previous.ExecuteOn(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.PostRemote(currentCommand);
                }

                public override async Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsyncOn(busSession);
                    var currentCommand = _next(previousResult);
                    return await busSession.PostRemoteAsync(currentCommand);
                }
            }

            internal class PostVoidCommand<TPrevious> : NavigationSpecification
            {
                readonly NavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, IExactlyOnceCommand> _next;
                internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, IExactlyOnceCommand> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override void ExecuteOn(IServiceBusSession busSession)
                {
                    var previousResult = _previous.ExecuteOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }

                public override async Task ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsyncOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }
            }
        }
    }
}
