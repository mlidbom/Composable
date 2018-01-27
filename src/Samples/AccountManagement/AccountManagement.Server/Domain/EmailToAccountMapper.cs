using AccountManagement.Domain.Events;
using Composable;
using Composable.Functional;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;
using AccountLink = Composable.Persistence.EventStore.EventStoreApi.Query.AggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain
{
    [UsedImplicitly] class EmailToAccountMapper
    {
        static DocumentDbApi DocumentDb => new ComposableApi().DocumentDb;

        internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email emailUpdated, ILocalServiceBusSession bus) =>
            {
                if(emailUpdated.AggregateVersion > 1)
                {
                    var previousAccountVersion = bus.GetLocal(AccountApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion - 1));
                    bus.PostLocal(DocumentDb.Commands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
                }

                var newEmail = emailUpdated.Email;
                bus.PostLocal(DocumentDb.Commands.Save(newEmail.ToString(), AccountApi.Queries.GetForUpdate(emailUpdated.AggregateId)));
            });

        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (AccountApi.Query.TryGetByEmailQuery tryGetAccount, ILocalServiceBusSession bus) =>
                bus.GetLocal(DocumentDb.Queries.TryGet<AccountLink>(tryGetAccount.Email.StringValue)) is Some<AccountLink> accountLink
                    ? Option.Some(accountLink.Value.GetLocalOn(bus))
                    : Option.None<Account>());
    }
}
