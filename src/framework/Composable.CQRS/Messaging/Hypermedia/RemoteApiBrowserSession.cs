using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.System.Threading;
using JetBrains.Annotations;

namespace Composable.Messaging.Hypermedia
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] class RemoteApiBrowserSession : IRemoteHypermediaNavigator
    {
        readonly IOutbox _transport;

        public RemoteApiBrowserSession(IOutbox transport) => _transport = transport;

        public void Post(MessageTypes.Remotable.AtMostOnce.ICommand command) => PostAsync(command).WaitUnwrappingException();

        public async Task PostAsync(MessageTypes.Remotable.AtMostOnce.ICommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            await _transport.DispatchAsync(command).NoMarshalling();
        }

        public TResult Post<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command) => PostAsync(command).ResultUnwrappingException();

        public async Task<TResult> PostAsync<TResult>(MessageTypes.Remotable.AtMostOnce.ICommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            CommandValidator.AssertCommandIsValid(command);
            return await _transport.DispatchAsync(command).NoMarshalling();
        }

        public async Task<TResult> GetAsync<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendRemote(query);
            return query is MessageTypes.ICreateMyOwnResultQuery<TResult> selfCreating
                       ? selfCreating.CreateResult()
                       : await _transport.DispatchAsync(query).NoMarshalling();
        }

        TResult IRemoteHypermediaNavigator.Get<TResult>(MessageTypes.Remotable.NonTransactional.IQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();
    }
}
