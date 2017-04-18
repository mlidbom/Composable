using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;
// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.DependencyInjection.Persistence
{
    public static class DocumentDbRegistrationExtensions
    {
        internal interface IDocumentDb<TUpdater, TReader, TBulkReader> : IDocumentDb
        {
        }

        [UsedImplicitly] class SqlServerDocumentDb<TUpdater, TReader, TBulkReader> : SqlServerDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
            public SqlServerDocumentDb(Lazy<string> connectionString) : base(connectionString)
            {
            }
        }

        [UsedImplicitly] class InMemoryDocumentDb<TUpdater, TReader, TBulkReader> : InMemoryDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
        }

        internal interface IDocumentDbSession<TUpdater, TReader, TBulkReader> : IDocumentDbSession { }

        [UsedImplicitly] class DocumentDbSession<TUpdater, TReader, TBulkReader> : DocumentDbSession, IDocumentDbSession<TUpdater, TReader, TBulkReader>
        {
            public DocumentDbSession(IDocumentDb<TUpdater, TReader, TBulkReader> backingStore, ISingleContextUseGuard usageGuard) : base(backingStore, usageGuard)
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

            if(@this.RunMode().IsTesting && @this.RunMode().Mode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .ImplementedBy<InMemoryDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .UsingFactoryMethod(kernel => new SqlServerDocumentDb<TUpdater, TReader, TBulkReader>(new Lazy<string>(() => kernel.Resolve<IConnectionStringProvider>()
                                                                                                                                                            .GetConnectionString(connectionName))))
                                         .LifestyleSingleton());
            }


            @this.Register(Component.For<IDocumentDbSession<TUpdater, TReader, TBulkReader>, IUnitOfWorkParticipant>()
                                     .ImplementedBy<DocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .LifestyleScoped());
            @this.Register(Component.For<TUpdater>(Seq.OfTypes<TUpdater, TReader, TBulkReader>())
                                    .UsingFactoryMethod(kernel => CreateProxyFor<TUpdater, TReader, TBulkReader>(kernel.Resolve<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()))
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
