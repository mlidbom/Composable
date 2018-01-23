using System;
using AccountManagement.Domain.Events;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AccountManagement.Domain.QueryModels
{
    class EmailToAccountIdQueryModel
    {
        EmailToAccountIdQueryModel() {}

        public EmailToAccountIdQueryModel(Email email, Guid accountId)
        {
            OldContract.Argument(() => email, () => accountId).NotNullOrDefault();

            Email = email;
            AccountId = accountId;
        }

        [JsonProperty]Email Email { [UsedImplicitly] get; set; }
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
                    var previousEmail = previousAccountVersion.Email;

                    if(previousEmail != null)
                    {
                        queryModels.Delete<EmailToAccountIdQueryModel>(previousEmail);
                    }
                }

                var newEmail = message.Email;
                queryModels.Save(newEmail, new EmailToAccountIdQueryModel(newEmail, message.AggregateId));
            });
    }
}
