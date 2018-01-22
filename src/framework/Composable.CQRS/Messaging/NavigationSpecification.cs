using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public abstract void ExecuteOn(IServiceBusSession busSession);
        public abstract Task ExecuteAsyncOn(IServiceBusSession busSession);

        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Local.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Local.StartCommand(command);

        public static NavigationSpecification<TResult> GetRemote<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Remote.StartQuery(query);
        public static NavigationSpecification<TResult> PostRemote<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Remote.StartCommand(command);
    }

    public abstract class NavigationSpecification<TResult>
    {
        public abstract TResult ExecuteOn(IServiceBusSession busSession);
        public abstract Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession);


        public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.Local.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.Local.PostCommand<TResult>(this, next);
        public NavigationSpecification Post(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new Local.PostVoidCommand<TResult>(this, next);

        public NavigationSpecification<TNext> GetRemote<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.Remote.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> PostRemote<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.Remote.PostCommand<TResult>(this, next);
        public NavigationSpecification PostRemote(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new Remote.PostVoidCommand<TResult>(this, next);

        class SelectQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, TResult> _select;

            internal SelectQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
            {
                _previous = previous;
                _select = select;
            }

            public override TResult ExecuteOn(IServiceBusSession busSession)
            {
                var previousResult = _previous.ExecuteOn(busSession);
                return _select(previousResult);
            }

            public override Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession) => Task.FromResult(ExecuteOn(busSession));
        }


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
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

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
