using AccountManagement.Domain.Events;
using Composable;
using Composable.Functional;
using Composable.Messaging.Buses;
using Composable.Messaging.Hypermedia;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;
using AccountLink = Composable.Persistence.EventStore.EventStoreApi.QueryApi.AggregateLink<AccountManagement.Domain.Account>;

namespace AccountManagement.Domain
{
    [UsedImplicitly] class EmailToAccountMapper
    {
        static DocumentDbApi DocumentDb => new ComposableApi().DocumentDb;

        internal static void UpdateMappingWhenEmailChanges(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForEvent(
            (AccountEvent.PropertyUpdated.Email emailUpdated, ILocalHypermediaNavigator navigator) =>
            {
                if(emailUpdated.AggregateVersion > 1)
                {
                    var previousAccountVersion = navigator.Execute(InternalApi.Queries.GetReadOnlyCopyOfVersion(emailUpdated.AggregateId, emailUpdated.AggregateVersion - 1));
                    navigator.Execute(DocumentDb.Commands.Delete<AccountLink>(previousAccountVersion.Email.StringValue));
                }

                var newEmail = emailUpdated.Email;
                navigator.Execute(DocumentDb.Commands.Save(newEmail.StringValue, InternalApi.Queries.GetForUpdate(emailUpdated.AggregateId)));
            });

        internal static void TryGetAccountByEmail(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (InternalApi.Query.TryGetByEmailQuery query, ILocalHypermediaNavigator navigator) =>
                navigator.Execute(DocumentDb.Queries.TryGet<AccountLink>(query.Email.StringValue)) is Some<AccountLink> accountLink
                    ? Option.Some(navigator.Execute(accountLink.Value))
                    : Option.None<Account>());
    }
}
