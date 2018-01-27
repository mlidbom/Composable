using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class RemoteNavigationSpecification<TResult>
    {
        public abstract TResult ExecuteRemoteOn(IRemoteServiceBusSession busSession);
        public abstract Task<TResult> ExecuteRemoteAsyncOn(IRemoteServiceBusSession busSession);

        public RemoteNavigationSpecification<TNext> Select<TNext>(Func<TResult, TNext> select) => new RemoteNavigationSpecification<TNext>.SelectQuery<TResult>(this, select);

        public RemoteNavigationSpecification<TNext> GetRemote<TNext>(Func<TResult, MessagingApi.IQuery<TNext>> next) => new RemoteNavigationSpecification<TNext>.Remote.ContinuationQuery<TResult>(this, next);
        public RemoteNavigationSpecification<TNext> PostRemote<TNext>(Func<TResult, MessagingApi.Remote.ExactlyOnce.ICommand<TNext>> next) => new RemoteNavigationSpecification<TNext>.Remote.PostCommand<TResult>(this, next);
        public RemoteNavigationSpecification PostRemote(Func<TResult, MessagingApi.Remote.ExactlyOnce.ICommand> next) => new Remote.PostVoidCommand<TResult>(this, next);

        class SelectQuery<TPrevious> : RemoteNavigationSpecification<TResult>
        {
            readonly RemoteNavigationSpecification<TPrevious> _previous;
            readonly Func<TPrevious, TResult> _select;

            internal SelectQuery(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, TResult> select)
            {
                _previous = previous;
                _select = select;
            }

            public override TResult ExecuteRemoteOn(IRemoteServiceBusSession busSession)
            {
                var previousResult = _previous.ExecuteRemoteOn(busSession);
                return _select(previousResult);
            }

            public override Task<TResult> ExecuteRemoteAsyncOn(IRemoteServiceBusSession busSession) => Task.FromResult(ExecuteRemoteOn(busSession));
        }
    }
}
