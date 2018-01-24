using System;
using AccountManagement.Domain.Events;
using Composable.Functional;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels
{
    [UsedImplicitly] class EmailToAccountMapper
    {
        class EmailToAccountMap
        {
            public EmailToAccountMap(Guid accountId) => AccountId = accountId;
            public Guid AccountId { get; private set; }
        }

        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (PrivateAccountApi.Query.TryGetByEmailQuery tryGetAccount, IDocumentDbReader documentDb, ILocalServiceBusSession bus) =>
                documentDb.TryGet(tryGetAccount.Email, out EmailToAccountMap map) ? Option.Some(bus.Get(PrivateAccountApi.Queries.ById(map.AccountId))) : Option.None<Account>());

        internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email message, IDocumentDbUpdater queryModels, ILocalServiceBusSession bus) =>
            {
                if(message.AggregateVersion > 1)
                {
                    var previousAccountVersion = bus.Get(PrivateAccountApi.Queries.ReadOnlyCopyOfVersion(message.AggregateId, message.AggregateVersion -1));
                    queryModels.Delete<EmailToAccountMap>(previousAccountVersion.Email);
                }

                var newEmail = message.Email;
                queryModels.Save(newEmail, new EmailToAccountMap(message.AggregateId));
            });
    }
}
