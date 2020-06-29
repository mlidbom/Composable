using Composable.Messaging;
using Composable.Refactoring.Naming;

namespace AccountManagement
{
    static class DomainTypeMapper
    {
        //In order to allow you to rename types when you need to composable does not use type names in the persisted data in the event store, document database or the service bus.
        //In order to enable you to freely rename and move types you must map each concrete type to a unique Guid.
        //To make this as easy as possible for you Composable will detect missing mappings and throw an exception telling exactly which lines of code you need to paste into the method below.
        //The lines you see here are pasted directly from the message in such an exception.
        public static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .Map<Domain.Account>("c2ca53e0-ee6d-4725-8bf8-c13b680d0ac5")
               .Map<Domain.Events.AccountEvent.Created>("3eb16cfa-ee90-4bec-a4fd-d6c52ebe0bbf")
               .Map<Domain.Events.AccountEvent.Implementation.LoggedIn>("e4cb1903-4e51-44f2-b866-43891d86cf94")
               .Map<Domain.Events.AccountEvent.Implementation.LoginFailed>("a659a369-584c-41e1-99ae-782b8a053b38")
               .Map<Domain.Events.AccountEvent.Implementation.Root>("7a98ea5a-aa91-43d2-b1bc-1b0d28842750")
               .Map<Domain.Events.AccountEvent.Implementation.UserChangedEmail>("4cc87a2c-3149-4748-87fe-1fde17b7473d")
               .Map<Domain.Events.AccountEvent.Implementation.UserChangedPassword>("eea04c7c-b51e-4669-947f-beb0f6b3fad6")
               .Map<Domain.Events.AccountEvent.Implementation.UserRegistered>("14d51523-1ede-41b4-aaef-6fde43f45d28")
               .Map<Domain.Events.AccountEvent.LoggedIn>("86761d19-29a5-4e88-9e02-6b17ce5d7be0")
               .Map<Domain.Events.AccountEvent.LoginFailed>("45db94ed-7114-47cd-82a5-3ea4cfdad975")
               .Map<Domain.Events.AccountEvent.PropertyUpdated.Email>("426f6b93-7af0-43b2-96ff-ddb613442e95")
               .Map<Domain.Events.AccountEvent.PropertyUpdated.Password>("d5666a12-33ab-489d-8b94-fddf4d2e7a15")
               .Map<Domain.Events.AccountEvent.Root>("86a2ff9c-c558-43ce-8a87-efaf49915275")
               .Map<Domain.Events.AccountEvent.UserChangedEmail>("1fe10abb-25b5-4243-b148-439b435002a5")
               .Map<Domain.Events.AccountEvent.UserChangedPassword>("0f7e4685-20d6-4f3e-ab32-9d153bbdbfee")
               .Map<Domain.Events.AccountEvent.UserRegistered>("2c648c9f-4860-46e3-a672-6d81ea35cd3f")
               .Map<Domain.Events.AccountEvent.LoginAttempted>("e6f64c0d-bd21-45d1-8737-4764912fc303")
               .Map<Composable.Persistence.EventStore.EventStoreApi.Query.AggregateLink<AccountManagement.Domain.Account>>("bae6bbcc-e69e-40ce-8872-d2683bfe4410")
               .Map<AccountManagement.UI.QueryModels.AccountStatistics.SingletonStatisticsQuerymodel>("2bc73c9f-df0a-4abd-b101-69e8ec7d01ec");
        }
    }
}
