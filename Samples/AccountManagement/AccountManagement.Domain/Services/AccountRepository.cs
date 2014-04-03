using AccountManagement.Domain.Events.EventStore;
using AccountManagement.Domain.Events.EventStore.Services;
using Composable.CQRS;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly]
    public class AccountRepository : AggregateRepository<Account>, IAccountRepository
    {
        public AccountRepository(IAccountManagementEventStoreSession aggregates) : base(aggregates) {}
    }
}