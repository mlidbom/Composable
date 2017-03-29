using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Persistence
{
    abstract class SqlServerDocumentDbRegistration
    {
        protected SqlServerDocumentDbRegistration(string description)
        {
            Contract.Argument(() => description)
                    .NotNullEmptyOrWhiteSpace();

            DocumentDbName = $"{description}.DocumentDb";
            SessionName = $"{description}.Session";
        }
        internal string DocumentDbName { get; }
        internal string SessionName { get; }
    }

    class SqlServerDocumentDbRegistration<TFactory> : SqlServerDocumentDbRegistration
    {
        public SqlServerDocumentDbRegistration() : base(typeof(TFactory).FullName) {}
    }

    public static class DocumentDbRegistrationExtensions
    {
        public static void RegisterSqlServerDocumentDb<TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TUpdater, TReader, TBulkReader>());

            var registration = new SqlServerDocumentDbRegistration<TUpdater>();

            var serviceLocator = @this.CreateServiceLocator();

            if(!@this.IsTestMode)
            {

                var connectionString = serviceLocator.Resolve<IConnectionStringProvider>()
                                                               .GetConnectionString(connectionName)
                                                               .ConnectionString;

                @this.Register(CComponent.For<IDocumentDb>()
                                        .UsingFactoryMethod(locator => new SqlServerDocumentDb(connectionString:connectionString))
                                        .Named(registration.DocumentDbName)
                                        .LifestyleScoped());
            } else
            {
                @this.Register(CComponent.For<IDocumentDb>()
                                        .ImplementedBy<InMemoryDocumentDb>()
                                        .Named(registration.DocumentDbName)
                                        .LifestyleSingleton());
            }


            @this.Register(CComponent.For<IDocumentDbSession>()
                                     .UsingFactoryMethod(locator => new DocumentDbSession(backingStore: serviceLocator.Resolve<IDocumentDb>(registration.DocumentDbName),
                                                                                          usageGuard: locator.Resolve<ISingleContextUseGuard>()))
                                     .Named(registration.SessionName)
                                     .LifestyleScoped());
            @this.Register(CComponent.For<TUpdater>(Seq.OfTypes<TUpdater, TReader, TBulkReader>())
                                    .UsingFactoryMethod(kernel => CreateProxyFor<TUpdater, TReader, TBulkReader>(kernel.Resolve<IDocumentDbSession>(registration.SessionName)))
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
