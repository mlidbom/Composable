using System;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IOutbox
    {
        Task SendAtTimeAsync(DateTime sendAt, IDomainCommand command);
        Task PublishAsync(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query);
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query);

        Task SendAsync(IDomainCommand command);
        Task<TResult> SendAsync<TResult>(IDomainCommand<TResult> command);
        void Start();
        void Stop();
    }


    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void Dispatch(IEvent message);
        void Dispatch(IDomainCommand command);
        Task<TCommandResult> DispatchAsync<TCommandResult>(IDomainCommand<TCommandResult> command);
        Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> command);
    }

    interface IInbox
    {
    }
}