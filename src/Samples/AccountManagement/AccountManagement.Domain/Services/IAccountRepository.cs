using Composable.Persistence.EventStore;

namespace AccountManagement.Domain.Services
{
    public interface IAccountRepository : IAggregateRepository<Account> {}
}
