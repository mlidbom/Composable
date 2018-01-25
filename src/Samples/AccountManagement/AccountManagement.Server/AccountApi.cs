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

            internal EntityByIdQuery<Account> ById(Guid id) => new EntityByIdQuery<Account>(id);

            internal ReadonlyCopyOfEntityByIdQuery<Account> ReadOnlyCopy(Guid id) => new ReadonlyCopyOfEntityByIdQuery<Account>(id);

            internal ReadonlyCopyOfEntityVersionByIdQuery<Account> ReadOnlyCopyOfVersion(Guid id, int version) => new ReadonlyCopyOfEntityVersionByIdQuery<Account>(id, version);

            internal class TryGetByEmailQuery : IQuery<Option<Account>>
            {
                public TryGetByEmailQuery(Email accountId)
                {
                    OldContract.Argument(() => accountId).NotNullOrDefault();
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
