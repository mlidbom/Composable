// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class BasicHandlerRegistration
    {
        void IllustratateRegistration()
        {
            var endpoint = new Endpoint(
                //Command handlers
                CommandHandler.For<CreateAccountCommand>("17893552-D533-4A59-A177-63EAF3B7B07E", command => {}),

                //Event handlers
                EventHandler.For<AccountCreatedEvent>("2E8642CA-6C60-4B91-A92E-54AD3753E7F2", @event => {})
            );
        }
    }
}
