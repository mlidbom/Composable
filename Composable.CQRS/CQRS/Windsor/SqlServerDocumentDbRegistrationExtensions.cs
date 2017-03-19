using System;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Contracts;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor
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
        public Dependency DocumentDb => Dependency.OnComponent(typeof(IDocumentDb), componentName: DocumentDbName);
    }

    class SqlServerDocumentDbRegistration<TFactory> : SqlServerDocumentDbRegistration
    {
        public SqlServerDocumentDbRegistration() : base(typeof(TFactory).FullName) {}
    }

    public static class DocumentDbRegistrationExtensions
    {
        public static void RegisterSqlServerDocumentDb<TSession, TUpdater, TReader, TBulkReader>(this IWindsorContainer @this,
                                                                                                 string connectionName)
            where TSession : IDocumentDbSession
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSession, TUpdater, TReader, TBulkReader>());

            var connectionString = Dependency.OnValue(typeof(string),
                                                      @this.Resolve<IConnectionStringProvider>()
                                                           .GetConnectionString(connectionName)
                                                           .ConnectionString);

            var registration = new SqlServerDocumentDbRegistration<TSession>();

            @this.Register(
                           Component.For<IDocumentDb>()
                                    .ImplementedBy<SqlServerDocumentDb>()
                                    .DependsOn(connectionString)
                                    .LifestylePerWebRequest()
                                    .Named(registration.DocumentDbName),
                           Component.For(Seq.OfTypes<IDocumentDbSession>())
                                    .ImplementedBy<DocumentDbSession>()
                                    .DependsOn(registration.DocumentDb)
                                    .LifestylePerWebRequest()
                                    .Named(registration.SessionName),
                           Component.For(Seq.OfTypes<TSession, TUpdater, TReader, TBulkReader>())
                                    .UsingFactoryMethod(kernel => CreateProxyFor<TSession, TUpdater, TReader, TBulkReader>(kernel.Resolve<IDocumentDbSession>(registration.SessionName)))
                          );

            @this.WhenTesting()
                 .ReplaceDocumentDb(registration.DocumentDbName);
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
