using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.Serialization;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.SystemExtensions.Threading;
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
            public SqlServerDocumentDb(ISqlConnection connection, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) : base(connection, timeSource, serializer)
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

        public static DocumentDbRegistrationBuilder RegisterSqlServerDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
        {
            Contract.Argument(() => connectionName).NotNullEmptyOrWhiteSpace();

            if(@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IDocumentDb>()
                                         .UsingFactoryMethod((IDocumentDbSerializer serializer) => new InMemoryDocumentDb(serializer))
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Component.For<IDocumentDb>()
                                         .UsingFactoryMethod((ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) => new SqlServerDocumentDb(new LazySqlServerConnection(new OptimizedLazy<string>(() => connectionProvider.GetConnectionProvider(connectionName).ConnectionString)), timeSource, serializer))
                                         .LifestyleSingleton());
            }


            @this.Register(Component.For<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                                    .UsingFactoryMethod((IDocumentDb documentDb) => new DocumentDbSession(documentDb))
                                    .LifestyleScoped());

            return new DocumentDbRegistrationBuilder();
        }

        public static void RegisterSqlServerDocumentDb<TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TUpdater : class, IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TUpdater, TReader, TBulkReader>());

            if(@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .UsingFactoryMethod((IDocumentDbSerializer serializer) => new InMemoryDocumentDb<TUpdater, TReader, TBulkReader>(serializer))
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .UsingFactoryMethod((ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource, IDocumentDbSerializer serializer) => new SqlServerDocumentDb<TUpdater, TReader, TBulkReader>(connectionProvider.GetConnectionProvider(connectionName), timeSource, serializer))
                                         .LifestyleSingleton());
            }


            @this.Register(Component.For<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .UsingFactoryMethod((IDocumentDb<TUpdater, TReader, TBulkReader> documentDb) => new DocumentDbSession<TUpdater, TReader, TBulkReader>(documentDb))
                                     .LifestyleScoped());

            var sessionType = DocumentDbSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType;
            var constructor = Constructor.Compile.ForReturnType<TUpdater>().WithImplementingType(sessionType).WithArguments<IInterceptor[], IDocumentDbSession>();

            var emptyInterceptorArray = new IInterceptor[0];

            @this.Register(Component.For<TUpdater, TReader, TBulkReader>()
                                    .UsingFactoryMethod(DocumentDbSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType,
                                                        kernel => constructor(emptyInterceptorArray, kernel.Resolve<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()))
                                    .LifestyleScoped()
                          );
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
