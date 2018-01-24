using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class InternalServices
        {
            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            {
                GetById(registrar);
                Save(registrar);
                GetReadonlyCopyOfLatestVersion(registrar);
                GetReadonlyCopyOfSpecificVersion(registrar);
            }

            static void Save(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
                (PersistEntityCommand<Account> command, IEventStoreUpdater updater) => updater.Save(command.Entity));

            static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (EntityByIdQuery<Account> query, IEventStoreUpdater updater) => updater.Get<Account>(query.Id));

            static void GetReadonlyCopyOfLatestVersion(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (ReadonlyCopyOfEntityByIdQuery<Account> query, IEventStoreReader reader) => reader.GetReadonlyCopy<Account>(query.Id));

            static void GetReadonlyCopyOfSpecificVersion(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
                (ReadonlyCopyOfEntityVersionByIdQuery<Account> query, IEventStoreReader reader) => reader.GetReadonlyCopyOfVersion<Account>(query.Id, query.Version));
        }
    }
}
