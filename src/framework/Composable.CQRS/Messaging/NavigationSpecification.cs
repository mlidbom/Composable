using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract class NavigationSpecification
    {
        public abstract void ExecuteOn(IServiceBusSession busSession);
        public abstract Task ExecuteAsyncOn(IServiceBusSession busSession);

        public static NavigationSpecification<TResult> Get<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Local.StartQuery(query);
        public static NavigationSpecification<TResult> Post<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Local.StartCommand(command);
        public static NavigationSpecification Post(ITransactionalExactlyOnceDeliveryCommand command) => new Local.VoidCommand(command);
        public static NavigationSpecification PostRemote(ITransactionalExactlyOnceDeliveryCommand command) => new Remote.VoidCommand(command);


        public static NavigationSpecification<TResult> GetRemote<TResult>(IQuery<TResult> query) => new NavigationSpecification<TResult>.Remote.StartQuery(query);
        public static NavigationSpecification<TResult> PostRemote<TResult>(ITransactionalExactlyOnceDeliveryCommand<TResult> command) => new NavigationSpecification<TResult>.Remote.StartCommand(command);

        static class Local
        {
            public class VoidCommand : NavigationSpecification
            {
                readonly ITransactionalExactlyOnceDeliveryCommand _command;

                public VoidCommand(ITransactionalExactlyOnceDeliveryCommand command) => _command = command;

                public override void ExecuteOn(IServiceBusSession busSession) => busSession.Post(_command);
                public override Task ExecuteAsyncOn(IServiceBusSession busSession)
                {
                    ExecuteOn(busSession);
                    return Task.CompletedTask;
                }
            }
        }

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
