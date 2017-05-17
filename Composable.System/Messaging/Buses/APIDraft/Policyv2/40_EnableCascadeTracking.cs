// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class EnableCascadeTracking
    {
        void IllustratateRegistration()
        {
            var endpoint = new Endpoint(
                //Command handlers
                CommandHandler.For<CreateAccountCommand>("17893552-D533-4A59-A177-63EAF3B7B07E", command => {},
                    //Being explicit about which events might be published let's the bus reason about possible cascade effects easily and thus guarantee consistency for queries.
                    //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
                    Policy.Publishes<IAccountEvent>()),

                EventHandler.For<AccountCreatedEvent>("A5A1DF35-982C-4962-A7DA-C98AC88633C0",@event => {},
                    //Being explicit about which commands might be sent let's the bus reason about possible cascade effects easily and thus guarantee consistency for queries.
                    //It also makes it possible to get an overview of the structure of a complete endpoint in one place.
                    Policy.Sends<SendAccountRegistrationWelcomeEmailCommand>()
                )
            );
        }
    }
}
