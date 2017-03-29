using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;
// ReSharper disable UnusedTypeParameter

namespace Composable.DependencyInjection.Persistence
{
    public static class DocumentDbRegistrationExtensions
    {
        interface ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader> : IDocumentDb
        {
        }

        [UsedImplicitly] class SomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader> : SqlServerDocumentDb, ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader>
        {
            public SomethingOrOtherSqlServerDocumentDb(string connectionString) : base(connectionString)
            {
            }
        }

        [UsedImplicitly] class SomethingOrOtherInMemoryDocumentDb<TUpdater, TReader, TBulkReader> : InMemoryDocumentDb, ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader>
        {
        }

        interface ISomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader> : IDocumentDbSession { }

        [UsedImplicitly] class SomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader> : DocumentDbSession, ISomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader>
        {
            public SomethingOrOtherDocumentDbSession(ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader> backingStore, ISingleContextUseGuard usageGuard) : base(backingStore, usageGuard)
            {
            }
        }

        public static void RegisterSqlServerDocumentDb<TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TUpdater, TReader, TBulkReader>());

            var serviceLocator = @this.CreateServiceLocator();

            if(@this.IsTestMode)
            {
                @this.Register(Component.For<ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .ImplementedBy<SomethingOrOtherInMemoryDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .LifestyleSingleton());

            } else
            {
                var connectionString = serviceLocator.Resolve<IConnectionStringProvider>()
                                                     .GetConnectionString(connectionName)
                                                     .ConnectionString;

                @this.Register(Component.For<ISomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .UsingFactoryMethod(kernel => new SomethingOrOtherSqlServerDocumentDb<TUpdater, TReader, TBulkReader>(connectionString))
                                         .LifestyleScoped());
            }


            @this.Register(Component.For<ISomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .ImplementedBy<SomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .LifestyleScoped());
            @this.Register(Component.For<TUpdater>(Seq.OfTypes<TUpdater, TReader, TBulkReader>())
                                    .UsingFactoryMethod(kernel => CreateProxyFor<TUpdater, TReader, TBulkReader>(kernel.Resolve<ISomethingOrOtherDocumentDbSession<TUpdater, TReader, TBulkReader>>()))
                                    .LifestyleScoped()
                          );
        }

        static TUpdater CreateProxyFor<TUpdater, TReader, TBulkReader>(IDocumentDbSession session)
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            var sessionType = EventStoreSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType;
            return (TUpdater)Activator.CreateInstance(sessionType, new IInterceptor[] {}, session);
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TUpdater, TReader, TBulkReader>
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            internal static readonly Type ProxyType =
                new DefaultProxyBuilder()
                    .CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IDocumentDbSession),
                                                                 additionalInterfacesToProxy: new[]
                                                                                              {
                                                                                                  typeof(TUpdater),
                                                                                                  typeof(TReader),
                                                                                                  typeof(TBulkReader)
                                                                                              },
                                                                 options: ProxyGenerationOptions.Default);
        }
    }
}
