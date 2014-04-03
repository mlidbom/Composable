using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Shared;
using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using JetBrains.Annotations;
using NServiceBus;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    [UsedImplicitly]
    public class EmailToAccountMapQueryModelUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    {
        private readonly IAccountManagementQueryModelUpdaterSession _documentDbModels;
        private readonly IAccountManagementDocumentDbReader _generatedModels;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementQueryModelUpdaterSession documentDbModels, IAccountManagementDocumentDbReader generatedModels)
        {
            _documentDbModels = documentDbModels;
            _generatedModels = generatedModels;
        }

        public void Handle(IAccountEmailPropertyUpdatedEvent message)
        {
            Email previousEmail;
            if(message.AggregateRootVersion > 1)
            {
                var previousAccountVersion = _generatedModels.Get<AccountQueryModel>(message.AggregateRootId);
                previousEmail = previousAccountVersion.Email;
                _documentDbModels.Delete<EmailToAccountMapQueryModel>(previousEmail);                
            }
            var newEmail = message.Email;
            _documentDbModels.Save(newEmail, new EmailToAccountMapQueryModel(newEmail, message.AggregateRootId));
        }
    }
}
