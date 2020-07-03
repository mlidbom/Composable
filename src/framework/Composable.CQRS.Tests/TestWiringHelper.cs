using System;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.Persistence.Common.DependencyInjection;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;

namespace Composable.Tests
{
    static class TestWiringHelper
    {
        static readonly string DocumentDbConnectionStringName = "Fake_connectionstring_for_database_testing";
        internal static string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

        internal static IEventStore EventStore(this IServiceLocator @this) =>
            @this.Resolve<IEventStore>();

        internal static IEventStore SqlEventStore(this IServiceLocator @this) =>
            @this.EventStore();//todo: Throw here if it is not the correct type of store

        internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
            @this.Resolve<IDocumentDb>();

        internal static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
            @this.Resolve<IDocumentDbReader>();

        internal static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
            @this.Resolve<IDocumentDbUpdater>();

        internal static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
            @this.Resolve<IDocumentDbBulkReader>();

        internal static IEventStoreUpdater EventStoreUpdater(this IServiceLocator @this) =>
            @this.Resolve<IEventStoreUpdater>();

        internal static IEventStoreReader EventStoreReader(this IServiceLocator @this) =>
            @this.Resolve<IEventStoreReader>();

        internal static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
            => @this.Resolve<IDocumentDbSession>();

        static void RegisterTestingDocumentDb(this IDependencyInjectionContainer @this)
        {
            @this.RegisterDocumentDb(DocumentDbConnectionStringName);
        }

        static void RegisterTestingEventStore(this IDependencyInjectionContainer @this)
        {
            @this.RegisterEventStore(EventStoreConnectionStringName);
        }

        internal static IServiceLocator SetupTestingServiceLocator([InstantHandle]Action<IEndpointBuilder> configureContainer = null)
        {
            return DependencyInjectionContainer.CreateServiceLocatorForTesting(container =>
                                                                               {
                                                                                   container.Container.RegisterTestingDocumentDb();
                                                                                   container.Container.RegisterTestingEventStore();
                                                                                   configureContainer?.Invoke(container);
                                                                               });
        }
    }
}