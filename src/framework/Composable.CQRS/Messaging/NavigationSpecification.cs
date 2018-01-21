using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        internal abstract void Execute(IRemoteServiceBusSession busSession);
        internal abstract Task ExecuteAsync(IRemoteServiceBusSession busSession);

        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Local.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Local.StartCommand(command);

        public static NavigationSpecification<TResult> GetRemote<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Remote.StartQuery(query);
        public static NavigationSpecification<TResult> PostRemote<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Remote.StartCommand(command);
    }

    public abstract class NavigationSpecification<TResult>
    {
        internal abstract TResult Execute(IRemoteServiceBusSession busSession);
        internal abstract Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession);

        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.Local.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.Local.PostCommand<TResult>(this, next);
        public NavigationSpecification Post(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new Local.PostVoidCommand<TResult>(this, next);

        public NavigationSpecification<TNext> GetRemote<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.Remote.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> PostRemote<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.Remote.PostCommand<TResult>(this, next);
        public NavigationSpecification PostRemote(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new Remote.PostVoidCommand<TResult>(this, next);


        internal static class Local
        {
            internal class StartQuery : NavigationSpecification<TResult>
            {
                readonly IQuery<TResult> _start;

                internal StartQuery(IQuery<TResult> start) => _start = start;

                internal override TResult Execute(IRemoteServiceBusSession busSession) => busSession.Get(_start);
                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => Task.FromResult(Execute(busSession));
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

                internal override TResult Execute(IRemoteServiceBusSession busSession) => busSession.Post(_start);
                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => Task.FromResult(Execute(busSession));
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

                internal override TResult Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return busSession.Get(currentQuery);
                }

                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => Task.FromResult(Execute(busSession));
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

                internal override TResult Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.Post(currentCommand);
                }

                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => Task.FromResult(Execute(busSession));
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

                internal override void Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.Post(currentCommand);
                }

                internal override Task ExecuteAsync(IRemoteServiceBusSession busSession)
                {
                    Execute(busSession);
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

                internal override TResult Execute(IRemoteServiceBusSession busSession) => busSession.GetRemote(_start);
                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => busSession.GetRemoteAsync(_start);
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

                internal override TResult Execute(IRemoteServiceBusSession busSession) => busSession.PostRemote(_start);
                internal override Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession) => busSession.PostRemoteAsync(_start);
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

                internal override TResult Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return busSession.GetRemote(currentQuery);
                }

                internal override async Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsync(busSession);
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

                internal override TResult Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.PostRemote(currentCommand);
                }

                internal override async Task<TResult> ExecuteAsync(IRemoteServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsync(busSession);
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

                internal override void Execute(IRemoteServiceBusSession busSession)
                {
                    var previousResult = _previous.Execute(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }

                internal override async Task ExecuteAsync(IRemoteServiceBusSession busSession)
                {
                    var previousResult = await _previous.ExecuteAsync(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }
            }
        }
    }

    public static class NavigationSpecificationExtensions
    {
        public static TResult Execute<TResult>(this IRemoteServiceBusSession @this, NavigationSpecification<TResult> specification) => specification.Execute(@this);
        public static async Task<TResult> ExecuteAsync<TResult>(this IRemoteServiceBusSession @this, NavigationSpecification<TResult> specification) => await specification.ExecuteAsync(@this);

        public static void Execute(this IRemoteServiceBusSession @this, NavigationSpecification specification) => specification.Execute(@this);
        public static async Task ExecuteAsync(this IRemoteServiceBusSession @this, NavigationSpecification specification) => await specification.ExecuteAsync(@this);
    }
}
