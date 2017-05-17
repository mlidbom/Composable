// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedMember.Local

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Composable.Messaging.Buses.APIDraft.Policyv2
{
    // ReSharper disable once UnusedMember.Global
    public class HandlerDependencies
    {
        void IllustratateRegistration()
        {
            var endpoint = new Endpoint(
                //Command handlers
                CommandHandler.For<CreateAccountCommand>(
                    "17893552-D533-4A59-A177-63EAF3B7B07E",
                    command => {},
                    //This handler must wait until there are no messages queued to any handler with policy:
                    //Policy.Updates<EmailToAccountLookupModel>. Throws an exception on registration if there are no handlers with matching Updates<> policy.
                    Policy.RequiresUpToDate<EmailToAccountLookupModel>.All),

                //This command handler is completely independent of any other handler since it just sends an email based on the data in the command.
                //It can run in parallel with any other handler and itself.
                CommandHandler.For<SendAccountRegistrationWelcomeEmailCommand>("76773E2F-9E44-4150-8C3C-8A4FC93899C3", command => {}, Policy.NoRestrictions),

                EventHandler.For<AccountCreatedEvent>(
                    "E59B41A3-BF32-4B7A-B497-F29E3AF42D42",
                    @event => {},
                    Policy.Updates<EmailToAccountLookupModel>.WithId(new ExtractEmailFromEmailUpdatedEvent()))
            );
        }
    }
}
