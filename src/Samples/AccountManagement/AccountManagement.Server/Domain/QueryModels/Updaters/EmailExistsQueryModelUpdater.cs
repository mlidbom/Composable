using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels.Updaters
{
    [UsedImplicitly]
    class EmailExistsQueryModelUpdater
    {
        readonly IAccountManagementDomainDocumentDbUpdater _querymodels;
        readonly IAccountRepository _repository;

        public EmailExistsQueryModelUpdater(IAccountManagementDomainDocumentDbUpdater querymodels, IAccountRepository repository)
        {
            _querymodels = querymodels;
            _repository = repository;
        }

        public void Handle(AccountEvent.PropertyUpdated.Email message)
        {
            if(message.AggregateRootVersion > 1)
            {
                var previousAccountVersion = _repository.GetReadonlyCopyOfVersion(message.AggregateRootId, message.AggregateRootVersion - 1);
                var previousEmail = previousAccountVersion.Email;

                if(previousEmail != null)
                {
                    _querymodels.Delete<EmailExistsQueryModel>(previousEmail);
                }
            }

            var newEmail = message.Email;
            _querymodels.Save(newEmail, new EmailExistsQueryModel(newEmail, message.AggregateRootId));
        }
    }
}
