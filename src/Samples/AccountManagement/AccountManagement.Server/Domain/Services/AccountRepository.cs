using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;
using AccountEvent = AccountManagement.Domain.Events.AccountEvent;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class AccountRepository : AggregateRepository<Account, AccountEvent.Implementation.Root, AccountEvent.Root>, IAccountRepository
    {
        public AccountRepository(IEventStoreUpdater aggregates, IEventStoreReader reader) : base(aggregates, reader) {}

        internal static void RegisterWith(IDependencyInjectionContainer container) =>
            container.Register(Component.For<IAccountRepository>().ImplementedBy<AccountRepository>().LifestyleScoped());
    }
}
