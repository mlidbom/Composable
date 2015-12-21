using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.ContainerInstallers.Testing
{
    [UsedImplicitly]
    public class InMemoryAccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IConfigureWiringForTests, IResetTestDatabases>()
                    .Instance(new DocumentDbTestConfigurer(container))
                );
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
                if(!_container.Kernel.HasComponent(QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller.ComponentKeys.InMemoryDocumentDb))
                {
                    _container.Register(
                        Component.For<IDocumentDb>()
                            .ImplementedBy<InMemoryDocumentDb>()
                            .Named(QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller.ComponentKeys.InMemoryDocumentDb)
                            .IsDefault()
                            .LifestyleSingleton());
                }

                _container.Kernel.AddHandlerSelector(
                    new KeyReplacementHandlerSelector(
                        typeof(IDocumentDb),
                        AccountManagementQuerymodelsSessionInstaller.ComponentKeys.KeyForDocumentDb,
                        QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller.ComponentKeys.InMemoryDocumentDb));
            }

            public void ResetDatabase()
            {
                _container.Resolve<InMemoryDocumentDb>(QueryModels.ContainerInstallers.AccountManagementDocumentDbReaderInstaller.ComponentKeys.InMemoryDocumentDb).Clear();
            }
        }
    }
}
