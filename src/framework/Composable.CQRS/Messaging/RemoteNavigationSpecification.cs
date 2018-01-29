using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class RemoteNavigationSpecification
    {
        public static RemoteNavigationSpecification Post(BusApi.RemoteSupport.AtMostOnce.ICommand command) => new RemoteNavigationSpecification.Remote.VoidCommand(command);
        internal static class Remote
        {
            public class VoidCommand : RemoteNavigationSpecification
            {
                readonly BusApi.RemoteSupport.AtMostOnce.ICommand _command;

                public VoidCommand(BusApi.RemoteSupport.AtMostOnce.ICommand command) => _command = command;

                public override void NavigateOn(IRemoteApiBrowser busSession) => busSession.Post(_command);
                public override Task NavigateOnAsync(IRemoteApiBrowser busSession)
                {
                    busSession.Post(_command);
                    return Task.CompletedTask;
                }
            }
        }
        public static RemoteNavigationSpecification<TResult> Get<TResult>(BusApi.RemoteSupport.NonTransactional.IQuery<TResult> query) => new RemoteNavigationSpecification<TResult>.Remote.StartQuery(query);
        public static RemoteNavigationSpecification<TResult> Post<TResult>(BusApi.RemoteSupport.AtMostOnce.ICommand<TResult> command) => new RemoteNavigationSpecification<TResult>.Remote.StartCommand(command);

        public abstract void NavigateOn(IRemoteApiBrowser busSession);
        public abstract Task NavigateOnAsync(IRemoteApiBrowser busSession);
    }
}
