using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.UI.QueryModels.DocumentDB.Updaters.Services;
using Composable.CQRS.EventHandling;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters
{
    [UsedImplicitly]
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
