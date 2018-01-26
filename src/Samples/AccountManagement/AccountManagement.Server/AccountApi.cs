using System;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging;

namespace AccountManagement
{
    static class AccountApi
    {
        internal static Query Queries => new Query();
        internal static Command Commands => new Command();

        internal class Query
        {
            internal TryGetByEmailQuery TryGetByEmail(Email email) => new TryGetByEmailQuery(email);

            internal AggregateLink<Account> Get(Guid id) => new AggregateLink<Account>(id);

            internal GetReadonlyCopyOfEntity<Account> GetReadOnlyCopy(Guid id) => new GetReadonlyCopyOfEntity<Account>(id);

            internal GetReadonlyCopyOfEntityVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => new GetReadonlyCopyOfEntityVersion<Account>(id, version);

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
            internal PersistEntityCommand<Account> SaveNew(Account account) => new PersistEntityCommand<Account>(account);
        }
    }
}
