using System.Threading.Tasks;
using Composable.Messaging.Buses.Implementation;
using Composable.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Composable.Messaging.Hypermedia
{
    //Todo: Build a pipeline to handle things like command validation, caching layers etc. Don't explicitly check for rules and optimization here with duplication across the class.
    [UsedImplicitly] class RemoteHypermediaNavigator : IRemoteHypermediaNavigator
    {
        readonly ITransport _transport;

        public RemoteHypermediaNavigator(ITransport transport) => _transport = transport;

        public void Post(IAtMostOnceHypermediaCommand command) => PostAsync(command).WaitUnwrappingException();

        public async Task PostAsync(IAtMostOnceHypermediaCommand command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            await _transport.PostAsync(command).NoMarshalling();
        }

        public TResult Post<TResult>(IAtMostOnceCommand<TResult> command) => PostAsync(command).ResultUnwrappingException();

        public async Task<TResult> PostAsync<TResult>(IAtMostOnceCommand<TResult> command)
        {
            MessageInspector.AssertValidToSendRemote(command);
            return await _transport.PostAsync(command).NoMarshalling();
        }

        public Task<TResult> GetAsync<TResult>(IRemotableQuery<TResult> query)
        {
            MessageInspector.AssertValidToSendRemote(query);
            if(query is ICreateMyOwnResultQuery<TResult> selfCreating)
                return Task.FromResult(selfCreating.CreateResult());

            return GetAsyncAfterFastPathOptimization(query);
        }
        async Task<TResult> GetAsyncAfterFastPathOptimization<TResult>(IRemotableQuery<TResult> query) => await _transport.GetAsync(query).NoMarshalling();

        TResult IRemoteHypermediaNavigator.Get<TResult>(IRemotableQuery<TResult> query) => GetAsync(query).ResultUnwrappingException();
    }
}
