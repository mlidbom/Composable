using System;
using AccountManagement.Domain.Events;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;

namespace AccountManagement.Domain
{
    [UsedImplicitly] class EmailToAccountMapper
    {
        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AccountApi.Query.TryGetByEmailQuery tryGetAccount, IDocumentDbReader documentDb, ILocalServiceBusSession bus) =>
                documentDb.TryGet(tryGetAccount.Email, out AggregateLink<Account> accountLink) ? Option.Some(bus.Get(accountLink)) : Option.None<Account>());

        internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email emailUpdated, IDocumentDbUpdater queryModels, ILocalServiceBusSession bus) =>
            {
                if(emailUpdated.AggregateVersion > 1)
                {
                    var previousAccountVersion = bus.Get(AccountApi.Queries.ReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion -1));
                    queryModels.Delete<AggregateLink<Account>>(previousAccountVersion.Email);
                }

                var newEmail = emailUpdated.Email;
                queryModels.Save(newEmail, new AggregateLink<Account>(emailUpdated.AggregateId));
            });
    }
}
