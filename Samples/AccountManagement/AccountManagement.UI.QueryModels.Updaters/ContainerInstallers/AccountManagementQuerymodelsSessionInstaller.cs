using AccountManagement.UI.QueryModels.Updaters.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.Updaters.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string KeyForDocumentDb = "AccountManagement.QueryModelUpdaters.IDocumentDb";
            public const string KeyForSession = "AccountManagement.QueryModelUpdaters.IDocumentDbSession";
            public const string KeyForNullOpSessionInterceptor = "AccountManagement.QueryModelUpdaters.NullOpSessionInterceptor";
        }

        public const string ConnectionStringName = QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ConnectionStringName;

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new { connectionString = GetConnectionStringFromConfiguration(ConnectionStringName) })
                    .Named(ComponentKeys.KeyForDocumentDb)
                    .LifestylePerWebRequest(),

                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .Named(ComponentKeys.KeyForNullOpSessionInterceptor)
                    .LifestyleSingleton(),

                Component.For<IDocumentDbSession, IAccountManagementQueryModelUpdaterSession>()
                    .ImplementedBy<AccountManagementQueryModelUpdaterSession>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), ComponentKeys.KeyForDocumentDb),
                        Dependency.OnComponent(typeof(IDocumentDbSessionInterceptor), ComponentKeys.KeyForNullOpSessionInterceptor))
                    .Named(ComponentKeys.KeyForSession)
                    .LifestylePerWebRequest(),

                    Component.For<IConfigureWiringForTests, IResetTestDatabases>()
                        .Instance(new DocumentDbTestConfigurer(container))
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }

        private class DocumentDbTestConfigurer : IConfigureWiringForTests, IResetTestDatabases
        {
            private readonly IWindsorContainer _container;

            public DocumentDbTestConfigurer(IWindsorContainer container)
            {
                _container = container;
            }

            public void ConfigureWiringForTesting()
            {
                //The ViewModelUpdatersSession and the ViewModelsSession must use the same document db for things to be sane.
                //Sometimes only the wiring for one is used. Sometimes the wiring for both. This if clause takes care of that issue.
                if(!_container.Kernel.HasComponent(QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb))
                {
                    _container.Register(
                        Component.For<IDocumentDb>()
                            .ImplementedBy<InMemoryDocumentDb>()
                            .Named(QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb)
                            .IsDefault()
                            .LifestyleSingleton());
                }

                _container.Kernel.AddHandlerSelector(
                        new KeyReplacementHandlerSelector(
                            typeof(IDocumentDb),
                            ComponentKeys.KeyForDocumentDb,
                            QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb));
            }

            public void ResetDatabase()
            {
                _container.Resolve<InMemoryDocumentDb>(QueryModels.ContainerInstallers.AccountManagementQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb).Clear();
            }
        }
    }
}
