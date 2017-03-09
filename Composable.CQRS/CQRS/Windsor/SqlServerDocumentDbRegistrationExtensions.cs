
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Contracts;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor.Testing;
// ReSharper disable UnusedMember.Global todo: write tests

namespace Composable.CQRS.Windsor
{
    public abstract class SqlServerDocumentDbRegistration
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
        public Dependency DocumentDbSession => Dependency.OnComponent(typeof(IDocumentDbSession), componentName: SessionName);
        public Dependency DocumentDbReader => Dependency.OnComponent(typeof(IDocumentDbReader), componentName: SessionName);
        public Dependency DocumentDbBulkReader => Dependency.OnComponent(typeof(IDocumentDbBulkReader), componentName: SessionName);
        public Dependency DocumentDbUpdater => Dependency.OnComponent(typeof(IDocumentDbUpdater), componentName: SessionName);

    }

    public class SqlServerDocumentDbRegistration<TFactory> : SqlServerDocumentDbRegistration
    {
        public SqlServerDocumentDbRegistration():base(typeof(TFactory).FullName) {}
    }

    public static class DocumentDbRegistrationExtensions
    {
        public static SqlServerDocumentDbRegistration RegisterSqlServerDocumentDb
            (this IWindsorContainer @this,
             SqlServerDocumentDbRegistration registration,
             string connectionName,
             Dependency sessionInterceptor = null)
        {
            Contract.Argument(() => registration)
                        .NotNull();
            Contract.Argument(() => connectionName)
                        .NotNullEmptyOrWhiteSpace();

            //We don't want to get any old interceptor that might have been registered by someone else.
            sessionInterceptor = sessionInterceptor ?? Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance);

            var connectionString = Dependency.OnValue(typeof(string),@this.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString);

            @this.Register(
                Component.For<IDocumentDb>()
                         .ImplementedBy<SqlServerDocumentDb>()
                         .DependsOn(connectionString)
                    .LifestylePerWebRequest()
                    .Named(registration.DocumentDbName),
                Component.For(Seq.OfTypes<IDocumentDbSession, IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader, IUnitOfWorkParticipant, IDocumentUpdatedNotifier>())
                         .ImplementedBy<DocumentDbSession>()
                         .DependsOn(registration.DocumentDb, sessionInterceptor)
                         .LifestylePerWebRequest()
                         .Named(registration.SessionName)
                );

            @this.WhenTesting()
                 .ReplaceDocumentDb(registration.DocumentDbName);

            return registration;
        }
    }
}
