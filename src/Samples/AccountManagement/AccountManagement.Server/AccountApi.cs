﻿using System;
using AccountManagement.Domain;
using AccountManagement.UI.QueryModels;
using Composable;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging;
using Composable.Persistence.EventStore;

// ReSharper disable MemberCanBeMadeStatic.Global we want _composable_ fluent APIs which does not happen with static members since we need instances to compose the API.

namespace AccountManagement
{
    static class AccountApi
    {
        static ComposableApi ComposableApi => new ComposableApi();
        internal static Query Queries => new Query();
        internal static Command Commands => new Command();
        internal static AccountQueryModel.Api AccountQueryModel => new AccountQueryModel.Api();

        internal class Query
        {
            internal TryGetByEmailQuery TryGetByEmail(Email email) => new TryGetByEmailQuery(email);

            internal EventStoreApi.Query.AggregateLink<Account> GetForUpdate(Guid id) => ComposableApi.EventStore.Queries.GetForUpdate<Account>(id);

            internal EventStoreApi.Query.GetReadonlyCopyOfAggregate<Account> GetReadOnlyCopy(Guid id) => ComposableApi.EventStore.Queries.GetReadOnlyCopy<Account>(id);

            internal EventStoreApi.Query.GetReadonlyCopyOfAggregateVersion<Account> GetReadOnlyCopyOfVersion(Guid id, int version) => ComposableApi.EventStore.Queries.GetReadOnlyCopyOfVersion<Account>(id, version);

            internal class TryGetByEmailQuery : MessageTypes.StrictlyLocal.IQuery<Option<Account>>
            {
                public TryGetByEmailQuery(Email accountId)
                {
                    Contract.ArgumentNotNullOrDefault(accountId, nameof(Account));
                    Email = accountId;
                }

                internal Email Email { get; private set; }
            }
        }

        internal class Command
        {
            internal EventStoreApi.Command.SaveAggregate<Account> Save(Account account) => ComposableApi.EventStore.Commands.Save(account);
        }
    }
}
