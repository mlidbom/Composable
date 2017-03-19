using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.EventStore.Services;
using AccountManagement.Domain.Events.Implementation;
using Composable.CQRS;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    interface IAccountManagementDomainDocumentDbSession : IDocumentDbSession { }

    interface IAccountManagementDomainDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementDomainDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementDomainDocumentDbBulkReader : IDocumentDbBulkReader { }

    [UsedImplicitly] class AccountRepository : AggregateRepository<Account, AccountEvent, IAccountEvent>, IAccountRepository
    {
        public AccountRepository(IAccountManagementEventStoreSession aggregates) : base(aggregates) {}
    }
}
