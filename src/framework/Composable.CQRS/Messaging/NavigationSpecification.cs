using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        internal abstract void Execute(IServiceBus bus);
        internal abstract Task ExecuteAsync(IServiceBus bus);

        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Local.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Local.StartCommand(command);

        public static NavigationSpecification<TResult> GetRemote<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Remote.StartQuery(query);
        public static NavigationSpecification<TResult> PostRemote<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Remote.StartCommand(command);
    }

    public abstract class NavigationSpecification<TResult>
    {
        internal abstract TResult Execute(IServiceBus bus);
        internal abstract Task<TResult> ExecuteAsync(IServiceBus bus);

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

                internal override TResult Execute(IServiceBus bus) => bus.Get(_start);
                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => Task.FromResult(Execute(bus));
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

                internal override TResult Execute(IServiceBus bus) => bus.Post(_start);
                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => Task.FromResult(Execute(bus));
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

                internal override TResult Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentQuery = _nextQuery(previousResult);
                    return bus.Get(currentQuery);
                }

                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => Task.FromResult(Execute(bus));
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

                internal override TResult Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentCommand = _next(previousResult);
                    return bus.Post(currentCommand);
                }

                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => Task.FromResult(Execute(bus));
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

                internal override void Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentCommand = _next(previousResult);
                    bus.Post(currentCommand);
                }

                internal override Task ExecuteAsync(IServiceBus bus)
                {
                    Execute(bus);
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

                internal override TResult Execute(IServiceBus bus) => bus.GetRemote(_start);
                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => bus.GetRemoteAsync(_start);
            }

            internal class StartCommand : NavigationSpecification<TResult>
            {
                readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

                internal StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

                internal override TResult Execute(IServiceBus bus) => bus.PostRemote(_start);
                internal override Task<TResult> ExecuteAsync(IServiceBus bus) => bus.PostRemoteAsync(_start);
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

                internal override TResult Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentQuery = _nextQuery(previousResult);
                    return bus.GetRemote(currentQuery);
                }

                internal override async Task<TResult> ExecuteAsync(IServiceBus bus)
                {
                    var previousResult = await _previous.ExecuteAsync(bus);
                    var currentQuery = _nextQuery(previousResult);
                    return await bus.GetRemoteAsync(currentQuery);
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

                internal override TResult Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentCommand = _next(previousResult);
                    return bus.PostRemote(currentCommand);
                }

                internal override async Task<TResult> ExecuteAsync(IServiceBus bus)
                {
                    var previousResult = await _previous.ExecuteAsync(bus);
                    var currentCommand = _next(previousResult);
                    return await bus.PostRemoteAsync(currentCommand);
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

                internal override void Execute(IServiceBus bus)
                {
                    var previousResult = _previous.Execute(bus);
                    var currentCommand = _next(previousResult);
                    bus.PostRemote(currentCommand);
                }

                internal override async Task ExecuteAsync(IServiceBus bus)
                {
                    var previousResult = await _previous.ExecuteAsync(bus);
                    var currentCommand = _next(previousResult);
                    bus.PostRemote(currentCommand);
                }
            }
        }
    }

    public static class NavigationSpecificationExtensions
    {
        public static TResult Execute<TResult>(this IServiceBus @this, NavigationSpecification<TResult> specification) => specification.Execute(@this);
        public static async Task<TResult> ExecuteAsync<TResult>(this IServiceBus @this, NavigationSpecification<TResult> specification) => await specification.ExecuteAsync(@this);

        public static void Execute(this IServiceBus @this, NavigationSpecification specification) => specification.Execute(@this);
        public static async Task ExecuteAsync(this IServiceBus @this, NavigationSpecification specification) => await specification.ExecuteAsync(@this);
    }
}
