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
        public static void RegisterSqlServerDocumentDb<TSession, TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TSession : IDocumentDbSession
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSession, TUpdater, TReader, TBulkReader>());

            var registration = new SqlServerDocumentDbRegistration<TSession>();

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
            @this.Register(CComponent.For<TSession>(Seq.OfTypes<TUpdater, TReader, TBulkReader>())
                                    .UsingFactoryMethod(kernel => CreateProxyFor<TSession, TUpdater, TReader, TBulkReader>(kernel.Resolve<IDocumentDbSession>(registration.SessionName)))
                                    .LifestyleScoped()
                          );
        }

        static TSession CreateProxyFor<TSession, TUpdater, TReader, TBulkReader>(IDocumentDbSession session)
            where TSession : IDocumentDbSession
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            var sessionType = EventStoreSessionProxyFactory<TSession, TUpdater, TReader, TBulkReader>.ProxyType;
            return (TSession)Activator.CreateInstance(sessionType, new IInterceptor[] {}, session);
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSession, TUpdater, TReader, TBulkReader>
            where TSession : IDocumentDbSession
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            internal static readonly Type ProxyType =
                new DefaultProxyBuilder()
                    .CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IDocumentDbSession),
                                                                 additionalInterfacesToProxy: new[]
                                                                                              {
                                                                                                  typeof(TSession),
                                                                                                  typeof(TUpdater),
                                                                                                  typeof(TReader),
                                                                                                  typeof(TBulkReader)
                                                                                              },
                                                                 options: ProxyGenerationOptions.Default);
        }
    }
}
