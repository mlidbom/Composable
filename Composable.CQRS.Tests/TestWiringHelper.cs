using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;

namespace Composable.CQRS.Tests
{
    interface ITestingEventstoreReader : IEventStoreReader { }

    interface ITestingEventstoreSession : IEventStoreSession{ }

    interface ITestingDocumentDbBulkReader : IDocumentDbBulkReader { }

    interface ITestingDocumentDbReader : IDocumentDbReader { }

    interface ITestingDocumentDbUpdater : IDocumentDbUpdater { }

    static class TestWiringHelper
    {
        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> EventStore(this IServiceLocator @this) =>
            @this.Resolve<IEventStore<ITestingEventstoreSession, ITestingEventstoreReader>>();

        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> SqlEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store

        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> InMemoryEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store


        internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
            @this.Resolve<DocumentDbRegistrationExtensions.IDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();


        internal static void RegisterTestingSqlServerDocumentDb(this IDependencyInjectionContainer @this)
        {
            @this.RegisterSqlServerDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>
                ("Fake_connectionstring_for_document_database_testing");
        }

        internal static void RegisterTestingSqlServerEventStore(this IDependencyInjectionContainer @this)
        {
            @this.RegisterSqlServerEventStore<ITestingEventstoreSession, ITestingEventstoreReader>
                ("Fake_connectionstring_for_event_store_testing");
        }

        internal static IServiceLocator SetupTestingServiceLocator(TestingMode mode)
        {
            return DependencyInjectionContainer.CreateServiceLocatorForTesting(container =>
                                                                               {
                                                                                   container.RegisterTestingSqlServerDocumentDb();
                                                                                   container.RegisterTestingSqlServerEventStore();
                                                                               },
                                                                               mode: mode);
        }
    }
}