using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Composable.Messaging.Buses.Implementation
{
    interface IOutbox
    {
        void SendAtTime(DateTime sendAt, ICommand command);
        void Publish(IEvent anEvent);
        TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult;
        Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult;

        void Send(ICommand command);
        Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage;
        void Start();
        void Stop();
    }


    interface IInterprocessTransport
    {
        void Stop();
        void Start();
        void Dispatch(IEvent message);
        void Dispatch(ICommand command);
        Task<TCommandResult> Dispatch<TCommandResult>(ICommand<TCommandResult> command) where TCommandResult : IMessage;
        Task<TQueryResult> Dispatch<TQueryResult>(IQuery<TQueryResult> command) where TQueryResult : IQueryResult;
    }

    interface IInbox
    {
        Task<object> Dispatch(IMessage message);
        string Address { get; }
    }
}