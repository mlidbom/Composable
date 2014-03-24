using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.UI.QueryModels.Updaters.Services;
using Composable.CQRS.EventHandling;

namespace AccountManagement.UI.QueryModels.Updaters
{
    public class AccountQueryModelUpdater : SingleAggregateQueryModelUpdater<AccountQueryModelUpdater, AccountQueryModel, IAccountEvent, IAccountManagementQueryModelUpdaterSession>
    {
        public AccountQueryModelUpdater(IAccountManagementQueryModelUpdaterSession session) : base(session)
        {
            RegisterHandlers()
                .For<IAccountEmailPropertyUpdatedEvent>(e => Model.Email = e.Email)
                .For<IAccountPasswordPropertyUpdateEvent>(e => Model.Password = e.Password);
        }
    }
}