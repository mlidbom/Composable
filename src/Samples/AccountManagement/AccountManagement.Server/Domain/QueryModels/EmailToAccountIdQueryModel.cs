using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using JetBrains.Annotations;

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

        public Email Email { [UsedImplicitly] get; private set; }
        public Guid AccountId { get; private set; }

        internal static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            registrar.ForEvent((AccountEvent.PropertyUpdated.Email message, IAccountManagementDomainDocumentDbUpdater queryModels, IAccountRepository repository) =>
                                   UpdateQueryModel(message, repository, queryModels))
                     .ForQuery((PrivateApi.Account.Queries.TryGetByEmailQuery tryGetAccount, IAccountManagementDomainDocumentDbReader documentDb, IAccountRepository accountRepository) =>
                                   TryGetAccountByEmail(tryGetAccount, documentDb, accountRepository));
        }

        static Option<Account> TryGetAccountByEmail(PrivateApi.Account.Queries.TryGetByEmailQuery tryGetAccount, IAccountManagementDomainDocumentDbReader documentDb, IAccountRepository accountRepository)
            => documentDb.TryGet(tryGetAccount.Email, out EmailToAccountIdQueryModel map) ? Option.Some(accountRepository.Get(map.AccountId)) : Option.None<Account>();

        static void UpdateQueryModel(AccountEvent.PropertyUpdated.Email message, IAccountRepository repository, IAccountManagementDomainDocumentDbUpdater queryModels)
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
        }
    }
}
