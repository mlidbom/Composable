using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Composable.ServiceBus;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    [UsedImplicitly]
    public class EmailToAccountMapQueryModelUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    {
        private readonly IAccountManagementQueryModelUpdaterSession _documentDbModels;
        private readonly IAccountManagementEventStoreGeneratedQueryModelsReader _generatedModels;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementQueryModelUpdaterSession documentDbModels,
            IAccountManagementEventStoreGeneratedQueryModelsReader generatedModels)
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
            _documentDbModels.Save(message.Email, new EmailToAccountMapQueryModel(message.Email, message.AggregateRootId));
        }
    }
}
