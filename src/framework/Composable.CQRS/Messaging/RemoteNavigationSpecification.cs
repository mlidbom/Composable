using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class RemoteNavigationSpecification
    {
        public static RemoteNavigationSpecification PostRemote(BusApi.RemoteSupport.AtMostOnce.ICommand command) => new RemoteNavigationSpecification.Remote.VoidCommand(command);
        internal static class Remote
        {
            public class VoidCommand : RemoteNavigationSpecification
            {
                readonly BusApi.RemoteSupport.AtMostOnce.ICommand _command;

                public VoidCommand(BusApi.RemoteSupport.AtMostOnce.ICommand command) => _command = command;

                public override void ExecuteRemoteOn(IUIInteractionApiBrowser busSession) => busSession.PostRemote(_command);
                public override Task ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession)
                {
                    busSession.PostRemote(_command);
                    return Task.CompletedTask;
                }
            }
        }
        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => new RemoteNavigationSpecification<TResult>.Remote.StartQuery(query);
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => new RemoteNavigationSpecification<TResult>.Remote.StartCommand(command);

        public abstract void ExecuteRemoteOn(IUIInteractionApiBrowser busSession);
        public abstract Task ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession);
    }
}
