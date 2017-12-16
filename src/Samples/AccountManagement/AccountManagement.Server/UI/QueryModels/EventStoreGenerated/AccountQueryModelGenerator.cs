using AccountManagement.Domain.Events;
using AccountManagement.Domain.Services;
using Composable.Persistence.EventStore.Query.Models.Generators;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStoreGenerated
{
    /// <summary>Ad hoc creates an <see cref="AccountQueryModel"/> by reading and applying the events from the event store reader</summary>
    [UsedImplicitly] class AccountQueryModelGenerator :
        SingleAggregateQueryModelGenerator<AccountQueryModelGenerator, AccountQueryModel, AccountEvent.Root, IAccountManagementEventStoreReader>,
        IAccountManagementQueryModelGenerator
    {
        //Note the use of a custom interface. This lets us keep query model generators for different systems apart in the wiring.
        public AccountQueryModelGenerator(IAccountManagementEventStoreReader session) : base(session)
        {
            RegisterHandlers()
                .For<AccountEvent.PropertyUpdated.Email>(e => Model.Email = e.Email)
                .For<AccountEvent.PropertyUpdated.Password>(e => Model.Password = e.Password);
        }
    }
}
