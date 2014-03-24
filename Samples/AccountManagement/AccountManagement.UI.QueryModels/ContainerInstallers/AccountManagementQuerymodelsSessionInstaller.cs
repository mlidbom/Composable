using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.CQRS.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string KeyForDocumentDb = "AccountManagement.QueryModels.IDocumentDb";
            public const string KeyForInMemoryDocumentDb = "AccountManagement.QueryModels.IDocumentDb.InMemory";
            public const string KeyForSession = "AccountManagement.QueryModels.IDocumentDbSession";
            public const string KeyForNullOpSessionInterceptor = "AccountManagement.QueryModels.NullOpSessionInterceptor";
        }

        public const string ConnectionStringName = "AccountManagementReadModels";

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

                Component.For<IDocumentDbSession, IAccountManagementQueryModelSession>()
                    .ImplementedBy<AccountManagementQueryModelSession>()
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
                if (!_container.Kernel.HasComponent(ComponentKeys.KeyForInMemoryDocumentDb))
                {
                    _container.Register(
                        Component.For<IDocumentDb>()
                            .ImplementedBy<InMemoryDocumentDb>()
                            .Named(ComponentKeys.KeyForInMemoryDocumentDb)
                            .IsDefault()
                            .LifestyleSingleton());
                }

                    _container.Kernel.AddHandlerSelector(
                        new KeyReplacementHandlerSelector(
                            typeof(IDocumentDb),
                            ComponentKeys.KeyForDocumentDb,
                            ComponentKeys.KeyForInMemoryDocumentDb));
            }

            public void ResetDatabase()
            {
                _container.Resolve<InMemoryDocumentDb>(ComponentKeys.KeyForInMemoryDocumentDb).Clear();
            }
        }
    }
}
