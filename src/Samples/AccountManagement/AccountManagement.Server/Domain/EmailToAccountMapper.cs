using AccountManagement.Domain.Events;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;

namespace AccountManagement.Domain
{
    [UsedImplicitly] class EmailToAccountMapper
    {
        internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email emailUpdated, IDocumentDbUpdater queryModels, ILocalServiceBusSession bus) =>
            {
                if(emailUpdated.AggregateVersion > 1)
                {
                    var previousAccountVersion = AccountApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion -1).GetLocalOn(bus);
                    queryModels.Delete<EventStoreApi.Query.AggregateLink<Account>>(previousAccountVersion.Email);
                }

                var newEmail = emailUpdated.Email;
                queryModels.Save(newEmail, new EventStoreApi.Query.AggregateLink<Account>(emailUpdated.AggregateId));
            });

        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AccountApi.Query.TryGetByEmailQuery tryGetAccount, IDocumentDbReader documentDb, ILocalServiceBusSession bus) =>
                documentDb.TryGet(tryGetAccount.Email, out EventStoreApi.Query.AggregateLink<Account> accountLink) ? Option.Some(accountLink.GetLocalOn(bus)) : Option.None<Account>());
    }
}
