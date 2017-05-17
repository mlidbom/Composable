// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class ModifyTheDefaults
    {
        void IllustratateRegistration()
        {
            var defaultEventHandlerPolicies = new CompositePolicy(
                Policy.LockExclusively.ThisHandler, //Ensures that this handler is never invoked in parallel with itself.
                Policy.LockExclusively.CurrentMessage //Ensures that no other handler handle the same message in parallel with this handler.
                //Useless when applied to a command handler since there can only be one.
            );

            var defaultCommandHandlerPolicies = new CompositePolicy(
                Policy.LockExclusively.AggregateRelatedToMessage
            );

            var endpoint = new Endpoint(
                //Command handlers
                CommandHandler.For<CreateAccountCommand>(
                    "17893552-D533-4A59-A177-63EAF3B7B07E",
                    command => {},
                    defaultCommandHandlerPolicies),

                //This command handler is completely independent of any other handler since it just sends an email based on the data in the command.
                //It can run in parallel with any other handler and itself.
                CommandHandler.For<SendAccountRegistrationWelcomeEmailCommand>("76773E2F-9E44-4150-8C3C-8A4FC93899C3", command => {}, Policy.NoRestrictions),

                //Event handlers
                EventHandler.For<AccountCreatedEvent>("2E8642CA-6C60-4B91-A92E-54AD3753E7F2", @event => {}, defaultEventHandlerPolicies)
            );
        }
    }
}
