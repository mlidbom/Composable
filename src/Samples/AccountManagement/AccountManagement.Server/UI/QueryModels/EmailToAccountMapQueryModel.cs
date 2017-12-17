using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.Messaging.Buses;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AccountManagement.UI.QueryModels
{
    class EmailToAccountMapQueryModel
    {
        [UsedImplicitly] EmailToAccountMapQueryModel() {}
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Email = email;
            AccountId = accountId;
        }

        [JsonProperty] Email Email { [UsedImplicitly] get; set; }
        [JsonProperty] internal Guid AccountId { get; private set; }


        internal static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            registrar.ForEvent((AccountEvent.PropertyUpdated.Email message, AccountManagementQueryModelReader generatedModels, IAccountManagementUiDocumentDbUpdater documentDbModels) =>
            {
                if (message.AggregateRootVersion > 1)
                {
                    var previousAccountVersion = generatedModels.GetAccount(message.AggregateRootId, message.AggregateRootVersion - 1);
                    documentDbModels.Delete<EmailToAccountMapQueryModel>(previousAccountVersion.Email);
                }
                documentDbModels.Save(message.Email, new EmailToAccountMapQueryModel(message.Email, message.AggregateRootId));
            });
        }
    }
}
