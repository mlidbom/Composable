using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;
using AccountEvent = AccountManagement.Domain.Events.AccountEvent;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class AccountRepository : AggregateRepository<Account, AccountEvent.Implementation.Root, AccountEvent.Root>, IAccountRepository
    {
        public AccountRepository(IEventStoreUpdater aggregates, IEventStoreReader reader) : base(aggregates, reader)
        {
        }
    }
}
