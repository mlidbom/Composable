using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class NavigationSpecification<TResult>
    {
        internal static class Local
        {
            internal class StartQuery : NavigationSpecification<TResult>
            {
                readonly IQuery<TResult> _start;

                internal StartQuery(IQuery<TResult> start) => _start = start;

                public override TResult ExecuteOn(IServiceBusSession busSession) => busSession.Get(_start);
                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => Task.FromResult(ExecuteOn(busSession));
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

                public override TResult ExecuteOn(IServiceBusSession busSession) => busSession.Post(_start);
                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => Task.FromResult(ExecuteOn(busSession));
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
                    return busSession.Get(currentQuery);
                }

                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => Task.FromResult(ExecuteOn(busSession));
            }

            internal class PostCommand<TPrevious> : NavigationSpecification<TResult>
            {
                readonly NavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> _next;
                internal PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override TResult ExecuteOn(IServiceBusSession busSession)
                {
                    var previousResult = _previous.ExecuteOn(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.Post(currentCommand);
                }

                public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => Task.FromResult(ExecuteOn(busSession));
            }

            internal class PostVoidCommand<TPrevious> : NavigationSpecification
            {
                readonly NavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> _next;
                internal PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override void ExecuteOn(IServiceBusSession busSession)
                {
                    var previousResult = _previous.ExecuteOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.Post(currentCommand);
                }

                public override Task ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    ExecuteOn(busSession);
                    return Task.CompletedTask;
                }
            }
        }
    }
}
