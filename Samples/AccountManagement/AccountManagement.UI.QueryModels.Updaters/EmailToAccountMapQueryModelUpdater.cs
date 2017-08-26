using AccountManagement.Domain.Events;
using AccountManagement.UI.QueryModels.Services;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    [UsedImplicitly]
    class EmailToAccountMapQueryModelUpdater
    {
        readonly IAccountManagementUiDocumentDbUpdater _documentDbModels;
        readonly IAccountManagementQueryModelsReader _generatedModels;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementUiDocumentDbUpdater documentDbModels,
            IAccountManagementQueryModelsReader generatedModels)
        {
            _documentDbModels = documentDbModels;
            _generatedModels = generatedModels;
        }

        public void Handle(AccountEvent.PropertyUpdated.Email message)
        {
            if(message.AggregateRootVersion > 1)
            {
                var previousAccountVersion = _generatedModels.GetAccount(message.AggregateRootId, message.AggregateRootVersion - 1);
                _documentDbModels.Delete<EmailToAccountMapQueryModel>(previousAccountVersion.Email);
            }
            _documentDbModels.Save(message.Email, new EmailToAccountMapQueryModel(message.Email, message.AggregateRootId));
        }
    }
}
