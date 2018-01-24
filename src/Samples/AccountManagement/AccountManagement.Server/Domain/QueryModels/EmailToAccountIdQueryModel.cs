using System;
using AccountManagement.Domain.Events;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Newtonsoft.Json;

namespace AccountManagement.Domain.QueryModels
{
    class EmailToAccountIdQueryModel
    {
        EmailToAccountIdQueryModel() {}

        public EmailToAccountIdQueryModel(Email email, Guid accountId)
        {
            OldContract.Argument(() => email, () => accountId).NotNullOrDefault();
            AccountId = accountId;
        }

        [JsonProperty]Guid AccountId { get; set; }

        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (PrivateAccountApi.Query.TryGetByEmailQuery tryGetAccount, IDocumentDbReader documentDb, ILocalServiceBusSession bus) =>
                documentDb.TryGet(tryGetAccount.Email, out EmailToAccountIdQueryModel map) ? Option.Some(bus.Get(PrivateAccountApi.Queries.ById(map.AccountId))) : Option.None<Account>());

        internal static void UpdateQueryModelWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email message, IDocumentDbUpdater queryModels, ILocalServiceBusSession bus) =>
            {
                if(message.AggregateVersion > 1)
                {
                    var previousAccountVersion = bus.Get(PrivateAccountApi.Queries.ReadOnlyCopyOfVersion(message.AggregateId, message.AggregateVersion -1));
                    queryModels.Delete<EmailToAccountIdQueryModel>(previousAccountVersion.Email);
                }

                var newEmail = message.Email;
                queryModels.Save(newEmail, new EmailToAccountIdQueryModel(newEmail, message.AggregateId));
            });
    }
}
