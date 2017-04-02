using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.System.Configuration;

namespace Composable.CQRS.Tests
{
    interface ITestingEventstoreReader : IEventStoreReader { }

    interface ITestingEventstoreSession : IEventStoreSession{ }

    interface ITestingDocumentDbBulkReader : IDocumentDbBulkReader { }

    interface ITestingDocumentDbReader : IDocumentDbReader { }

    interface ITestingDocumentDbUpdater : IDocumentDbUpdater { }

    static class TestWiringHelper
    {
        static string DocumentDbConnectionStringName = "Fake_connectionstring_for_document_database_testing";
        static string EventStoreConnectionStringName = "Fake_connectionstring_for_event_store_testing";

        internal static string DocumentDbConnectionString(this IServiceLocator @this) => @this.Resolve<IConnectionStringProvider>()
                                                                                              .GetConnectionString(DocumentDbConnectionStringName)
                                                                                              .ConnectionString;

        internal static string EventStoreConnectionString(this IServiceLocator @this) => @this.Resolve<IConnectionStringProvider>()
                                                                                              .GetConnectionString(EventStoreConnectionStringName)
                                                                                              .ConnectionString;

        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> EventStore(this IServiceLocator @this) =>
            @this.Resolve<IEventStore<ITestingEventstoreSession, ITestingEventstoreReader>>();

        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> SqlEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store

        internal static IEventStore<ITestingEventstoreSession, ITestingEventstoreReader> InMemoryEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store


        internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
            @this.Resolve<DocumentDbRegistrationExtensions.IDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();

        internal static ITestingDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbReader>();

        internal static ITestingDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbUpdater>();

        internal static ITestingDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbBulkReader>();

        internal static DocumentDbRegistrationExtensions.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader> DocumentDbSession(this IServiceLocator @this)
            => @this.Resolve<DocumentDbRegistrationExtensions.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();


        internal static void RegisterTestingSqlServerDocumentDb(this IDependencyInjectionContainer @this)
        {
            @this.RegisterSqlServerDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>
                (DocumentDbConnectionStringName);
        }

        internal static void RegisterTestingSqlServerEventStore(this IDependencyInjectionContainer @this)
        {
            @this.RegisterSqlServerEventStore<ITestingEventstoreSession, ITestingEventstoreReader>
                (EventStoreConnectionStringName);
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