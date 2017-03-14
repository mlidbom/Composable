using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels.Updaters
{
    [UsedImplicitly]
    class EmailToAccountMapQueryModelUpdater : IEventSubscriber<IAccountEmailPropertyUpdatedEvent>
    {
        readonly IAccountManagementDomainDocumentDbSession _querymodels;
        readonly IAccountRepository _repository;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementDomainDocumentDbSession querymodels, IAccountRepository repository)
        {
            _querymodels = querymodels;
            _repository = repository;
        }

        public void Handle(IAccountEmailPropertyUpdatedEvent message)
        {
            if(message.AggregateRootVersion > 1)
            {
                var previousAccountVersion = _repository.GetVersion(message.AggregateRootId, message.AggregateRootVersion - 1);
                var previousEmail = previousAccountVersion.Email;

                if(previousEmail != null)
                {
                    _querymodels.Delete<EmailToAccountMapQueryModel>(previousEmail);
                }
            }

            var newEmail = message.Email;
            _querymodels.Save(newEmail, new EmailToAccountMapQueryModel(newEmail, message.AggregateRootId));
        }
    }
}
