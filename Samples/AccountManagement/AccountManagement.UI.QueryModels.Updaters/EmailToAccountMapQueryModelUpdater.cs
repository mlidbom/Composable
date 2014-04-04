using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using JetBrains.Annotations;
using NServiceBus;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    [UsedImplicitly]
    public class EmailToAccountMapQueryModelUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    {
        private readonly IAccountManagementQueryModelUpdaterSession _documentDbModels;
        private readonly IAccountManagementQueryModelGeneratingDocumentDbReader _generatedModels;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementQueryModelUpdaterSession documentDbModels,
            IAccountManagementQueryModelGeneratingDocumentDbReader generatedModels)
        {
            _documentDbModels = documentDbModels;
            _generatedModels = generatedModels;
        }

        public void Handle(IAccountEmailPropertyUpdatedEvent message)
        {
            if(message.AggregateRootVersion > 1)
            {
                var previousAccountVersion = _generatedModels.GetVersion<AccountQueryModel>(message.AggregateRootId, message.AggregateRootVersion - 1);
                _documentDbModels.Delete<EmailToAccountMapQueryModel>(previousAccountVersion.Email);
            }
            var newEmail = message.Email;
            _documentDbModels.Save(newEmail, new EmailToAccountMapQueryModel(newEmail, message.AggregateRootId));
        }
    }
}
