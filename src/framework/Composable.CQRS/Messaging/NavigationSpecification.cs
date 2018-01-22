using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public abstract void ExecuteOn(IServiceBusSession busSession);
        public abstract Task ExecuteAsyncOn(IServiceBusSession busSession);

        public static NavigationSpecification Post(ITransactionalExactlyOnceDeliveryCommand command) => new Remote.VoidCommand(command);


        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Remote.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Remote.StartCommand(command);

        internal static class Remote
        {
            public class VoidCommand : NavigationSpecification
            {
                readonly ITransactionalExactlyOnceDeliveryCommand _command;

                public VoidCommand(ITransactionalExactlyOnceDeliveryCommand command) => _command = command;

                public override void ExecuteOn(IServiceBusSession busSession) => busSession.PostRemote(_command);
                public override Task ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    busSession.PostRemote(_command);
                    return Task.CompletedTask;
                }
            }
        }

    }
}
