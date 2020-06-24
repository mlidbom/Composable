using System;
using Composable.DependencyInjection;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using Composable.Persistence.SqlServer.DependencyInjection;
using JetBrains.Annotations;

namespace Composable.Tests
{
    interface ITestingEventStoreReader : IEventStoreReader { }

    interface ITestingEventStoreUpdater : IEventStoreUpdater { }

    interface ITestingDocumentDbBulkReader : IDocumentDbBulkReader { }

    interface ITestingDocumentDbReader : IDocumentDbReader { }

    interface ITestingDocumentDbUpdater : IDocumentDbUpdater { }

    static class TestWiringHelper
    {
        static readonly string DocumentDbConnectionStringName = "Fake_connectionstring_for_database_testing";
        internal static string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

        internal static IEventStore<ITestingEventStoreUpdater, ITestingEventStoreReader> EventStore(this IServiceLocator @this) =>
            @this.Resolve<IEventStore<ITestingEventStoreUpdater, ITestingEventStoreReader>>();

        internal static IEventStore<ITestingEventStoreUpdater, ITestingEventStoreReader> SqlEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store

        internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
            @this.Resolve<DocumentDbRegistrar.IDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();

        internal static ITestingDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbReader>();

        internal static ITestingDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbUpdater>();

        internal static ITestingDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingDocumentDbBulkReader>();

        internal static ITestingEventStoreUpdater EventStoreUpdater(this IServiceLocator @this) =>
            @this.Resolve<ITestingEventStoreUpdater>();

        internal static ITestingEventStoreReader EventStoreReader(this IServiceLocator @this) =>
            @this.Resolve<ITestingEventStoreReader>();

        internal static DocumentDbRegistrar.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader> DocumentDbSession(this IServiceLocator @this)
            => @this.Resolve<DocumentDbRegistrar.IDocumentDbSession<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>>();

        static void RegisterTestingDocumentDb(this IDependencyInjectionContainer @this)
        {
            @this.RegisterDocumentDb<ITestingDocumentDbUpdater, ITestingDocumentDbReader, ITestingDocumentDbBulkReader>(DocumentDbConnectionStringName);
        }

        static void RegisterTestingEventStore(this IDependencyInjectionContainer @this)
        {
            @this.RegisterEventStore<ITestingEventStoreUpdater, ITestingEventStoreReader>(EventStoreConnectionStringName);
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