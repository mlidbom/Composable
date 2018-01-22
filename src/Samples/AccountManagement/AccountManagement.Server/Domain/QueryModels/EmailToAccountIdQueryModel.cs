using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
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
            (PrivateApi.Account.Queries.TryGetByEmailQuery tryGetAccount, IDocumentDbReader documentDb, IAccountRepository accountRepository) =>
                documentDb.TryGet(tryGetAccount.Email, out EmailToAccountIdQueryModel map) ? Option.Some(accountRepository.Get(map.AccountId)) : Option.None<Account>());

        internal static void UpdateQueryModelWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email message, IDocumentDbUpdater queryModels, IAccountRepository repository) =>
            {
                if(message.AggregateRootVersion > 1)
                {
                    var previousAccountVersion = repository.GetReadonlyCopyOfVersion(message.AggregateRootId, message.AggregateRootVersion - 1);
                    var previousEmail = previousAccountVersion.Email;

                    if(previousEmail != null)
                    {
                        queryModels.Delete<EmailToAccountIdQueryModel>(previousEmail);
                    }
                }

                var newEmail = message.Email;
                queryModels.Save(newEmail, new EmailToAccountIdQueryModel(newEmail, message.AggregateRootId));
            });
    }
}
