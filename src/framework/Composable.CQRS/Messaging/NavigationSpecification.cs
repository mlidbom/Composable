using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public static NavigationSpecification Post(BusApi.RemoteSupport.AtMostOnce.ICommand command) => new VoidCommand(command);

        public static NavigationSpecification<TResult> Get<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => NavigationSpecification<TResult>.Get(query);
        public static NavigationSpecification<TResult> Post<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => NavigationSpecification<TResult>.Post(command);

        public abstract void NavigateOn(IRemoteApiBrowserSession busSession);
        public abstract Task NavigateOnAsync(IRemoteApiBrowserSession busSession);

        class VoidCommand : NavigationSpecification
        {
            readonly BusApi.RemoteSupport.AtMostOnce.ICommand _command;

            public VoidCommand(BusApi.RemoteSupport.AtMostOnce.ICommand command) => _command = command;

            public override void NavigateOn(IRemoteApiBrowserSession busSession) => busSession.Post(_command);
            public override Task NavigateOnAsync(IRemoteApiBrowserSession busSession)
            {
                busSession.Post(_command);
                return Task.CompletedTask;
            }
        }
    }
}
