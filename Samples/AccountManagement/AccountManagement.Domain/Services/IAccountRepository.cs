using Composable.CQRS;

namespace AccountManagement.Domain.Services
{
    public interface IAccountRepository : IAggregateRepository<Account>
    {
    }
}
