using Composable.Persistence.EventStore;

namespace AccountManagement.Domain.Services
{
    interface IAccountRepository : IAggregateRepository<Account> {}
}
