using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses.Implementation
{
    interface IInterprocessTransport
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
}