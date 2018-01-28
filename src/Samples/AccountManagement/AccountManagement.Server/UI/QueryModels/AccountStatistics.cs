using System;
using AccountManagement.Domain.Events;
using Composable.DDD;
using Composable.Messaging.Buses;
using Composable.Messaging.Events;
using Composable.Persistence.DocumentDb;

namespace AccountManagement.UI.QueryModels
{
    class AccountStatistics
    {
        public class QueryModel : IHasPersistentIdentity<Guid>
        {
            public int NumberOfAccounts { get; internal set; }
            public int NumberOfLoginsAttempts { get; internal set; }
            public int NumberOfSuccessfulLogins { get; internal set; }
            public int NumberOfFailedLogins { get; internal set; }
            public Guid Id => StaticId;

            internal static Guid StaticId = Guid.Parse("93498554-5C2E-4D6A-862D-2DA7BCCAC747");
        }


        internal static void Register(IEndpointBuilder builder)
        {
            RegisterHandlers(builder.RegisterHandlers);
        }

        internal static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.Root @event, ILocalServiceBusSession bus) =>
            {
                var eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<AccountEvent.Root>();

                QueryModel model = null;

                eventDispatcher.Register()
                               .BeforeHandlers(_ => model = bus.GetLocal(new DocumentDbApi().Queries.GetForUpdate<QueryModel>(QueryModel.StaticId)))
                               .For<AccountEvent.Created>(created => model.NumberOfAccounts++)
                               .For<AccountEvent.LoginAttempted>(loginAttempted => model.NumberOfLoginsAttempts++)
                               .For<AccountEvent.LoggedIn>(loggedIn => model.NumberOfSuccessfulLogins++)
                               .For<AccountEvent.LoginFailed>(loginFailed => model.NumberOfFailedLogins++);
            });
    }
}
