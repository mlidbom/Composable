using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] class RemoteApiBrowserSession : IRemoteApiNavigatorSession
    {
        readonly IInterprocessTransport _transport;

        public RemoteApiBrowserSession(IInterprocessTransport transport) => _transport = transport;

        void IRemoteApiNavigatorSession.Post(BusApi.Remotable.AtMostOnce.ICommand command) => ((IRemoteApiNavigatorSession)this).PostAsync(command).WaitUnwrappingException();

        async Task IRemoteApiNavigatorSession.PostAsync(BusApi.Remotable.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command);
        }

        TResult IRemoteApiNavigatorSession.Post<TResult>(BusApi.Remotable.AtMostOnce.ICommand<TResult> command) => ((IRemoteApiNavigatorSession)this).PostAsync(command).ResultUnwrappingException();

        async Task<TResult> IRemoteApiNavigatorSession.PostAsync<TResult>(BusApi.Remotable.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        async Task<TResult> IRemoteApiNavigatorSession.GetAsync<TResult>(BusApi.Remotable.NonTransactional.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendRemote(query);
            return query is BusApi.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteApiNavigatorSession.Get<TResult>(BusApi.Remotable.NonTransactional.IQuery<TResult> query) => ((IRemoteApiNavigatorSession)this).GetAsync(query).ResultUnwrappingException();
    }
}
