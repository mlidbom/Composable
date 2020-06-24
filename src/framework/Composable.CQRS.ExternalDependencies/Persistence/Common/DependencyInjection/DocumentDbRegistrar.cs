using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.InMemory.DocumentDB;
using Composable.Persistence.SqlServer.Configuration;
using Composable.Persistence.SqlServer.DocumentDb.SqlServer;
using Composable.Persistence.SqlServer.SystemExtensions;
using Composable.Serialization;
using Composable.System.Linq;
using Composable.System.Reflection;
using JetBrains.Annotations;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.Persistence.Common.DependencyInjection
{
    //urgent: Remove persistence layer registration from this class.
    public static class DocumentDbRegistrar
    {
        internal interface IDocumentDb<TUpdater, TReader, TBulkReader> : IDocumentDb
        {
        }

        [UsedImplicitly] class SqlServerDocumentDb<TUpdater, TReader, TBulkReader> : SqlServerDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
            public SqlServerDocumentDb(ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) : base(connectionProvider, timeSource, serializer)
            {
            }
        }

        [UsedImplicitly] class InMemoryDocumentDb<TUpdater, TReader, TBulkReader> : InMemoryDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
            public InMemoryDocumentDb(IDocumentDbSerializer serializer) : base(serializer) {}
        }

        internal interface IDocumentDbSession<TUpdater, TReader, TBulkReader> : IDocumentDbSession { }

        [UsedImplicitly] class DocumentDbSession<TUpdater, TReader, TBulkReader> : DocumentDbSession, IDocumentDbSession<TUpdater, TReader, TBulkReader>
        {
            public DocumentDbSession(IDocumentDb<TUpdater, TReader, TBulkReader> backingStore) : base(backingStore)
            {
            }
        }

        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IEndpointBuilder @this)
            => @this.Container.RegisterDocumentDb(@this.Configuration.ConnectionStringName);

        public static DocumentDbRegistrationBuilder RegisterDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
        {
            Contract.Argument(connectionName, nameof(connectionName)).NotNullEmptyOrWhiteSpace();

            //urgent: remove
            if(@this.RunMode.TestingPersistenceLayer == PersistenceLayer.InMemory)
            {
                @this.Register(Singleton.For<IDocumentDb>()
                                         .CreatedBy((IDocumentDbSerializer serializer) => new InMemoryDocumentDb(serializer))
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Singleton.For<IDocumentDb>()
                                         .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) => new SqlServerDocumentDb(connectionProviderSource.GetConnectionProvider(connectionName), timeSource, serializer)));
            }


            @this.Register(Scoped.For<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                                    .CreatedBy((IDocumentDb documentDb) => new DocumentDbSession(documentDb)));

            return new DocumentDbRegistrationBuilder();
        }

        public static void RegisterDocumentDb<TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TUpdater : class, IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(connectionName, nameof(connectionName))
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TUpdater, TReader, TBulkReader>());

            //urgent: remove
            if(@this.RunMode.TestingPersistenceLayer == PersistenceLayer.InMemory)
            {
                @this.Register(Singleton.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .CreatedBy((IDocumentDbSerializer serializer) => new InMemoryDocumentDb<TUpdater, TReader, TBulkReader>(serializer))
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                //urgent: move to SqlServerPersistenceLayerRegistrar
                @this.Register(Singleton.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .CreatedBy((ISqlServerConnectionProviderSource connectionProviderSource, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) => new SqlServerDocumentDb<TUpdater, TReader, TBulkReader>(connectionProviderSource.GetConnectionProvider(connectionName), timeSource, serializer)));
            }


            @this.Register(Scoped.For<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .CreatedBy((IDocumentDb<TUpdater, TReader, TBulkReader> documentDb) => new DocumentDbSession<TUpdater, TReader, TBulkReader>(documentDb)));

            var sessionType = DocumentDbSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType;
            var constructor = Constructor.Compile.ForReturnType<TUpdater>().WithImplementingType(sessionType).WithArguments<IInterceptor[], IDocumentDbSession>();

            var emptyInterceptorArray = new IInterceptor[0];

            @this.Register(Scoped.For<TUpdater, TReader, TBulkReader>()
                                    .CreatedBy(DocumentDbSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType,
                                                        kernel => constructor(emptyInterceptorArray, kernel.Resolve<IDocumentDbSession<TUpdater, TReader, TBulkReader>>())));
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class DocumentDbSessionProxyFactory<TUpdater, TReader, TBulkReader>
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

    public class DocumentDbRegistrationBuilder
    {
        public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            DocumentDbApi.HandleDocumentType<TDocument>(registrar);
            return this;
        }
    }
}
