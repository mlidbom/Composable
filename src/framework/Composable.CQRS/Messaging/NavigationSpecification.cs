using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public abstract void Execute(IServiceBus bus);
        public abstract Task ExecuteAsync(IServiceBus bus);

        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.StartCommand(command);
    }

    public abstract class NavigationSpecification<TResult>
    {
        public abstract TResult Execute(IServiceBus bus);
        public abstract Task<TResult> ExecuteAsync(IServiceBus bus);

        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);
        public NavigationSpecification Post(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new PostVoidCommand<TResult>(this, next);

        internal class StartQuery : NavigationSpecification<TResult>
        {
            readonly IQuery<TResult> _start;

            public StartQuery(IQuery<TResult> start) => _start = start;

            public override TResult Execute(IServiceBus bus) => bus.Query(_start);
            public override Task<TResult> ExecuteAsync(IServiceBus bus) => bus.QueryAsync(_start);
        }

        internal class StartCommand : NavigationSpecification<TResult>
        {
            readonly ITransactionalExactlyOnceDeliveryCommand<TResult> _start;

            public StartCommand(ITransactionalExactlyOnceDeliveryCommand<TResult> start) => _start = start;

            public override TResult Execute(IServiceBus bus) => bus.Send(_start);
            public override Task<TResult> ExecuteAsync(IServiceBus bus) => bus.SendAsync(_start);
        }

        class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, IQuery<TResult>> _nextQuery;

            public ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IQuery<TResult>> nextQuery)
            {
                _previous = previous;
                _nextQuery = nextQuery;
            }

            public override TResult Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentQuery = _nextQuery(previousResult);
                return bus.Query(currentQuery);
            }

            public override async Task<TResult> ExecuteAsync(IServiceBus bus)
            {
                var previousResult = await _previous.ExecuteAsync(bus);
                var currentQuery = _nextQuery(previousResult);
                return await bus.QueryAsync(currentQuery);
            }
        }

        class PostCommand<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> _next;
            public PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> next)
            {
                _previous = previous;
                _next = next;
            }

            public override TResult Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentCommand = _next(previousResult);
                return bus.Send(currentCommand);
            }

            public override async Task<TResult> ExecuteAsync(IServiceBus bus)
            {
                var previousResult = await _previous.ExecuteAsync(bus);
                var currentCommand = _next(previousResult);
                return await bus.SendAsync(currentCommand);
            }
        }

        class PostVoidCommand<TPrevious> : NavigationSpecification
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> _next;
            public PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> next)
            {
                _previous = previous;
                _next = next;
            }

            public override void Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentCommand = _next(previousResult);
                bus.Send(currentCommand);
            }

            public override async Task ExecuteAsync(IServiceBus bus)
            {
                var previousResult = await _previous.ExecuteAsync(bus);
                var currentCommand = _next(previousResult);
                bus.Send(currentCommand);
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
