// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class ParameterInjection
    {
        void IllustratateRegistration()
        {
            var endpoint = new Endpoint(
                //Command handlers
                CommandHandler.For("17893552-D533-4A59-A177-63EAF3B7B07E", (CreateAccountCommand command, AccountController controller) => controller.Handle(command)),


                //Delegate to container registered component to handle the event.
                EventHandler.For("6E0EA0E6-67DB-4D25-AFE5-99E67130773D", (AccountCreatedEvent @event, AccountController controller) => controller.Handle(@event)),

                //Generic parameter injection. Actually the same thing as the example above..
                EventHandler.For("85966417-20B9-4373-9A4B-8398ECA86429", (AccountCreatedEvent @event, AccountController dependency1, ISomeDependency dependency2) => {})
            );
        }
    }
}
