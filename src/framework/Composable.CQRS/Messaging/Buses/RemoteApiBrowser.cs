using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.Messaging.Hypermedia;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] class RemoteApiBrowserSession : IRemoteHypermediaNavigator
    {
        readonly IOutbox _transport;

        public RemoteApiBrowserSession(IOutbox transport) => _transport = transport;

        void IRemoteHypermediaNavigator.Post(MessageTypes.Remotable.AtMostOnce.ICommand command) => ((IRemoteHypermediaNavigator)this).PostAsync(command).WaitUnwrappingException();

        async Task IRemoteHypermediaNavigator.PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command).NoMarshalling();
        }

        TResult IRemoteHypermediaNavigator.Post<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => ((IRemoteHypermediaNavigator)this).PostAsync(command).ResultUnwrappingException();

        async Task<TResult> IRemoteHypermediaNavigator.PostAsync<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        async Task<TResult> IRemoteHypermediaNavigator.GetAsync<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendRemote(query);
            return query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteHypermediaNavigator.Get<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => ((IRemoteHypermediaNavigator)this).GetAsync(query).ResultUnwrappingException();
    }
}
