using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels.Updaters
{
    [UsedImplicitly]
    class EmailToAccountMapQueryModelUpdater : IEventSubscriber<AccountEvent.PropertyUpdated.Email>
    {
        readonly IAccountManagementDomainDocumentDbUpdater _querymodels;
        readonly IAccountRepository _repository;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementDomainDocumentDbUpdater querymodels, IAccountRepository repository)
        {
            _querymodels = querymodels;
            _repository = repository;
        }

        public void Handle(AccountEvent.PropertyUpdated.Email message)
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
