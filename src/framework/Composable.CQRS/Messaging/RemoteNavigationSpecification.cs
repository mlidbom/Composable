using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class RemoteNavigationSpecification
    {
        public static RemoteNavigationSpecification<TResult> GetRemote<TResult>(BusApi.IQuery<TResult> query) => new RemoteNavigationSpecification<TResult>.Remote.StartQuery(query);
        public static RemoteNavigationSpecification PostRemote(BusApi.Remote.ExactlyOnce.ICommand command) => new RemoteNavigationSpecification.Remote.VoidCommand(command);
        public static RemoteNavigationSpecification<TResult> PostRemote<TResult>(BusApi.Remote.ExactlyOnce.ICommand<TResult> command) => new RemoteNavigationSpecification<TResult>.Remote.StartCommand(command);

        public abstract void ExecuteRemoteOn(IRemoteServiceBusSession busSession);
        public abstract Task ExecuteRemoteAsyncOn(IRemoteServiceBusSession busSession);

        internal static class Remote
        {
            public class VoidCommand : RemoteNavigationSpecification
            {
                readonly BusApi.Remote.ExactlyOnce.ICommand _command;

                public VoidCommand(BusApi.Remote.ExactlyOnce.ICommand command) => _command = command;

                public override void ExecuteRemoteOn(IRemoteServiceBusSession busSession) => busSession.PostRemote(_command);
                public override Task ExecuteRemoteAsyncOn(IRemoteServiceBusSession busSession)
                {
                    busSession.PostRemote(_command);
                    return Task.CompletedTask;
                }
            }
        }

    }
}
