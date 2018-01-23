using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class NavigationSpecification<TResult>
    {
        public abstract TResult ExecuteOn(IServiceBusSession busSession);
        public abstract Task<TResult> ExecuteAsyncOn(IServiceBusSession busSession);

        public NavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new NavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public NavigationSpecification<TNext> Get<TNext>(Func<TResult, IQuery<TNext>> next) => new NavigationSpecification<TNext>.Remote.ContinuationQuery<TResult>(this, next);
        public NavigationSpecification<TNext> Post<TNext>(Func<TResult, IExactlyOnceCommand<TNext>> next) => new NavigationSpecification<TNext>.Remote.PostCommand<TResult>(this, next);
        public NavigationSpecification Post(Func<TResult, IExactlyOnceCommand> next) => new Remote.PostVoidCommand<TResult>(this, next);

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
    }
}
