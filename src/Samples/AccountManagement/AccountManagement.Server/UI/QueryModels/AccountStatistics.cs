using System;
using AccountManagement.Domain.Events;
using Composable.DDD;
using Composable.Functional;
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

        static readonly object _initializationlock = new object();
        static bool IsInitialized;
        static readonly DocumentDbApi DocumentDbApi = new DocumentDbApi();
        static void EnsureInitialized(ILocalServiceBusSession bus)
        {
            lock(_initializationlock)
            {
                if(!IsInitialized)
                {
                    if(bus.GetLocal(DocumentDbApi.Queries.TryGet<QueryModel>(QueryModel.StaticId)) is None<QueryModel>)
                    {
                        bus.PostLocal(DocumentDbApi.Commands.Save(new QueryModel()));
                    }
                }
            }
        }

        internal static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.Root @event, ILocalServiceBusSession bus) =>
            {
                EnsureInitialized(bus);
                var eventDispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<AccountEvent.Root>();

                QueryModel model = null;

                eventDispatcher.Register()
                               .BeforeHandlers(_ => model = bus.GetLocal(DocumentDbApi.Queries.GetForUpdate<QueryModel>(QueryModel.StaticId)))
                               .IgnoreUnhandled<AccountEvent.Root>()
                               .For<AccountEvent.Created>(created => model.NumberOfAccounts++)
                               .For<AccountEvent.LoginAttempted>(loginAttempted => model.NumberOfLoginsAttempts++)
                               .For<AccountEvent.LoggedIn>(loggedIn => model.NumberOfSuccessfulLogins++)
                               .For<AccountEvent.LoginFailed>(loginFailed => model.NumberOfFailedLogins++);

                eventDispatcher.Dispatch(@event);
            });
    }
}
