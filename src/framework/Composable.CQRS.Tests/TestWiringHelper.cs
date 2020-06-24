using System;
using Composable.DependencyInjection;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;

namespace Composable.Tests
{
    interface ITestingDocumentDbBulkReader : IDocumentDbBulkReader { }

    interface ITestingDocumentDbReader : IDocumentDbReader { }

    interface ITestingDocumentDbUpdater : IDocumentDbUpdater { }

    static class TestWiringHelper
    {
        static readonly string DocumentDbConnectionStringName = "Fake_connectionstring_for_database_testing";
        internal static string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

        internal static IEventStore EventStore(this IServiceLocator @this) =>
            @this.Resolve<IEventStore>();

        internal static IEventStore SqlEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store

        internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
            @this.Resolve<DocumentDbRegistrar.IDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();

        internal static ITestingDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbReader>();

        internal static ITestingDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbUpdater>();

        internal static ITestingDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbBulkReader>();

        internal static IEventStoreUpdater EventStoreUpdater(this IServiceLocator @this) =>
            @this.Resolve<IEventStoreUpdater>();

        internal static IEventStoreReader EventStoreReader(this IServiceLocator @this) =>
            @this.Resolve<IEventStoreReader>();

        internal static DocumentDbRegistrar.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader> DocumentDbSession(this IServiceLocator @this)
            => @this.Resolve<DocumentDbRegistrar.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();

        static void RegisterTestingDocumentDb(this IDependencyInjectionContainer @this)
        {
            @this.RegisterDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>(DocumentDbConnectionStringName);
        }

        static void RegisterTestingEventStore(this IDependencyInjectionContainer @this)
        {
            @this.RegisterEventStore<IEventStoreUpdater, IEventStoreReader>(EventStoreConnectionStringName);
        }

        internal static IServiceLocator SetupTestingServiceLocator([InstantHandle]Action<IDependencyInjectionContainer> configureContainer = null)
        {
            return DependencyInjectionContainer.CreateServiceLocatorForTesting(container =>
                                                                               {
                                                                                   container.RegisterTestingDocumentDb();
                                                                                   container.RegisterTestingEventStore();
                                                                                   configureContainer?.Invoke(container);
                                                                               });
        }
    }
}