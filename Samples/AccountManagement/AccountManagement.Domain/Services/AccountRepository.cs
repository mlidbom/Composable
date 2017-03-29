using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.EventStore.Services;
using AccountManagement.Domain.Events.Implementation;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    interface IAccountManagementDomainDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementDomainDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementDomainDocumentDbBulkReader : IDocumentDbBulkReader { }

    [UsedImplicitly] class AccountRepository : AggregateRepository<Account, AccountEvent, IAccountEvent>, IAccountRepository
    {
        public AccountRepository(IAccountManagementEventStoreSession aggregates) : base(aggregates) {}
    }
}
