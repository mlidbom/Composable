using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        internal abstract void Execute(IServiceBus bus);
        internal abstract Task ExecuteAsync(IServiceBus bus);
        public static NavigationSpecification<TCurrent> Get<TCurrent>(IQuery<TCurrent> query) => new NavigationSpecification<TCurrent>.StartQuery(query);
    }

    public abstract class NavigationSpecification<TResult>
    {
        internal abstract TResult Execute(IServiceBus bus);
        internal abstract Task<TResult> ExecuteAsync(IServiceBus bus);

        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, ITransactionalExactlyOnceDeliveryCommand<TNext>> next) => new NavigationSpecification<TNext>.PostCommand<TResult>(this, next);
        public NavigationSpecification Post(Func<TResult, ITransactionalExactlyOnceDeliveryCommand> next) => new NavigationSpecification<TResult>.PostVoidCommand<TResult>(this, next);

        internal class StartQuery : NavigationSpecification<TResult>
        {
            readonly IQuery<TResult> _start;

            public StartQuery(IQuery<TResult> start) => _start = start;

            internal override TResult Execute(IServiceBus bus) => bus.Query(_start);
            internal override Task<TResult> ExecuteAsync(IServiceBus bus) => bus.QueryAsync(_start);
        }

        internal class ContinuationQuery<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, IQuery<TResult>> _nextQuery;

            public ContinuationQuery(NavigationSpecification<TPrevious> previous, Func<TPrevious, IQuery<TResult>> nextQuery)
            {
                _previous = previous;
                _nextQuery = nextQuery;
            }

            internal override TResult Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentQuery = _nextQuery(previousResult);
                return bus.Query(currentQuery);
            }

            internal override async Task<TResult> ExecuteAsync(IServiceBus bus)
            {
                var previousResult = await _previous.ExecuteAsync(bus);
                var currentQuery = _nextQuery(previousResult);
                return await bus.QueryAsync(currentQuery);
            }
        }

        internal class PostCommand<TPrevious> : NavigationSpecification<TResult>
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> _next;
            public PostCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand<TResult>> next)
            {
                _previous = previous;
                _next = next;
            }

            internal override TResult Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentCommand = _next(previousResult);
                return bus.Send(currentCommand);
            }

            internal override async Task<TResult> ExecuteAsync(IServiceBus bus)
            {
                var previousResult = await _previous.ExecuteAsync(bus);
                var currentCommand = _next(previousResult);
                return await bus.SendAsync(currentCommand);
            }
        }

        internal class PostVoidCommand<TPrevious> : NavigationSpecification
        {
            readonly NavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> _next;
            public PostVoidCommand(NavigationSpecification<TPrevious> previous, Func<TPrevious, ITransactionalExactlyOnceDeliveryCommand> next)
            {
                _previous = previous;
                _next = next;
            }

            internal override void Execute(IServiceBus bus)
            {
                var previousResult = _previous.Execute(bus);
                var currentCommand = _next(previousResult);
                bus.Send(currentCommand);
            }

            internal override async Task ExecuteAsync(IServiceBus bus)
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
        public async static Task<TResult> ExecuteAsync<TResult>(this IServiceBus @this, NavigationSpecification<TResult> specification) => await specification.ExecuteAsync(@this);

        public static void Execute(this IServiceBus @this, NavigationSpecification specification) => specification.Execute(@this);
        public static async Task ExecuteAsync(this IServiceBus @this, NavigationSpecification specification) => await specification.ExecuteAsync(@this);
    }
}
