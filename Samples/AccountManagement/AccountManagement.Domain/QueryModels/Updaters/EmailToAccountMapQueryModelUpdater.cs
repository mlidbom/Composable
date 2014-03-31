using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Services;
using JetBrains.Annotations;
using NServiceBus;

namespace AccountManagement.Domain.QueryModels.Updaters
{
    [UsedImplicitly]
    public class EmailToAccountMapQueryModelUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    {
        private readonly IAccountManagementDomainQueryModelSession _querymodels;
        private readonly IAccountRepository _repository;

        public EmailToAccountMapQueryModelUpdater(IAccountManagementDomainQueryModelSession querymodels, IAccountRepository repository)
        {
            _querymodels = querymodels;
            _repository = repository;
        }

        public void Handle(IAccountEmailPropertyUpdatedEvent message)
        {
            var previousAccountVersion = _repository.GetVersion(message.AggregateRootId, message.AggregateRootVersion - 1);
            var previousEmail = previousAccountVersion.Email;
            var newEmail = message.Email;

            if(previousEmail != null)
            {
                _querymodels.Delete<EmailToAccountMapQueryModel>(previousEmail);
            }
            _querymodels.Save(newEmail, new EmailToAccountMapQueryModel(newEmail, message.AggregateRootId));
        }
    }
}
