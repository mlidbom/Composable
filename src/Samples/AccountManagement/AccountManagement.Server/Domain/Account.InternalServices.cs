using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class Repository
        {
            internal static void Save(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (PersistEntityCommand<Account> command, IEventStoreUpdater updater) => updater.Save(command.Entity));

            internal static void Get(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (AggregateLink<Account> query, IEventStoreUpdater updater) => updater.Get<Account>(query.Id));

            internal static void GetReadonlyCopyOfLatestVersion(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (ReadonlyCopyOfEntityByIdQuery<Account> query, IEventStoreReader reader) => reader.GetReadonlyCopy<Account>(query.Id));

            internal static void GetReadonlyCopyOfSpecificVersion(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (ReadonlyCopyOfEntityVersionByIdQuery<Account> query, IEventStoreReader reader) => reader.GetReadonlyCopyOfVersion<Account>(query.Id, query.Version));
        }
    }
}
