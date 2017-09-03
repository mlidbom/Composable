// ReSharper disable All

namespace Composable.Tests.Messaging.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class ParameterInjection
    {
        void IllustratateRegistration()
        {
            var endpoint = new Endpoint(
                EventHandler.For("85966417-20B9-4373-9A4B-8398ECA86429", (AccountCreatedEvent @event, AccountController dependency1, ISomeDependency dependency2) => {})
            );
        }
    }
}
