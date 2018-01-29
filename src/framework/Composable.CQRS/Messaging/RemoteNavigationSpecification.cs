using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class RemoteNavigationSpecification
    {
        public static RemoteNavigationSpecification PostRemote(BusApi.Remote.AtMostOnce.ICommand command) => new RemoteNavigationSpecification.Remote.VoidCommand(command);
        internal static class Remote
        {
            public class VoidCommand : RemoteNavigationSpecification
            {
                readonly BusApi.Remote.AtMostOnce.ICommand _command;

                public VoidCommand(BusApi.Remote.AtMostOnce.ICommand command) => _command = command;

                public override void ExecuteRemoteOn(IUIInteractionApiBrowser busSession) => busSession.PostRemote(_command);
                public override Task ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession)
                {
                    busSession.PostRemote(_command);
                    return Task.CompletedTask;
                }
            }
        }
        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(BusApi.Remote.NonTransactional.IQuery<TResult> query) => new RemoteNavigationSpecification<TResult>.Remote.StartQuery(query);
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(BusApi.Remote.AtMostOnce.ICommand<TResult> command) => new RemoteNavigationSpecification<TResult>.Remote.StartCommand(command);

        public abstract void ExecuteRemoteOn(IUIInteractionApiBrowser busSession);
        public abstract Task ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession);
    }
}
