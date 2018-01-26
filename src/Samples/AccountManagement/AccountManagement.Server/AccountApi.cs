using System;
using AccountManagement.Domain;
using Composable;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging;
// ReSharper disable MemberCanBeMadeStatic.Global we want composable fluent API which does not happen with static members.

namespace AccountManagement
{
    static class AccountApi
    {
        internal static Query Queries => new Query();
        internal static Command Commands => new Command();

        internal class Query
        {
            internal TryGetByEmailQuery TryGetByEmail(Email email) => new TryGetByEmailQuery(email);

            internal AggregateLink<Account> GetForUpdate(Guid id) => ComposableApi.EventStoreManaging<Account>.GetForUpdate(id);

            internal GetReadonlyCopyOfAggregate<Account> GetReadOnlyCopy(Guid id) => ComposableApi.EventStoreManaging<Account>.GetReadOnlyCopy(id);

            internal GetReadonlyCopyOfAggregateVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => ComposableApi.EventStoreManaging<Account>.GetReadOnlyCopyOfVersion(id, version);

            internal class TryGetByEmailQuery : IQuery<Option<Account>>
            {
                public TryGetByEmailQuery(Email accountId)
                {
                    Contract.Argument(() => accountId).NotNullOrDefault();
                    Email = accountId;
                }

                public Email Email { get; private set; }
            }
        }

        internal class Command
        {
            internal SaveAggregate<Account> Save(Account account) => ComposableApi.EventStoreManaging<Account>.Save(account);
        }
    }
}
