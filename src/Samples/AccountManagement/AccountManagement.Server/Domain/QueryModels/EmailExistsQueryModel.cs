using System;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using Composable.Contracts;
using Composable.Messaging.Buses;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels
{
    class EmailExistsQueryModel
    {
        EmailExistsQueryModel() { }

        public EmailExistsQueryModel(Email email, Guid accountId)
        {
            OldContract.Argument(() => email, () => accountId).NotNullOrDefault();

            Email = email;
        }

        Email Email { [UsedImplicitly] get; set; }


        internal static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) =>
            registrar.ForEvent((AccountEvent.PropertyUpdated.Email message, IAccountManagementDomainDocumentDbUpdater queryModels, IAccountRepository repository) =>
            {
                if(message.AggregateRootVersion > 1)
                {
                    var previousAccountVersion = repository.GetReadonlyCopyOfVersion(message.AggregateRootId, message.AggregateRootVersion - 1);
                    var previousEmail = previousAccountVersion.Email;

                    if(previousEmail != null)
                    {
                        queryModels.Delete<EmailExistsQueryModel>(previousEmail);
                    }
                }

                var newEmail = message.Email;
                queryModels.Save(newEmail, new EmailExistsQueryModel(newEmail, message.AggregateRootId));
            });
    }
}
