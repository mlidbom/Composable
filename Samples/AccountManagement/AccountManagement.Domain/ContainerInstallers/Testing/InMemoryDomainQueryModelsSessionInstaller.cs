using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Windsor.Testing;
using Composable.KeyValueStorage;
using Composable.Windsor;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers.Testing
{
    [UsedImplicitly]
    public class InMemoryDomainQueryModelsSessionInstaller : IWindsorInstaller
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
                _container.Register(
                    Component.For<IDocumentDb>()
                        .ImplementedBy<InMemoryDocumentDb>()
                        .Named(AccountManagementDomainQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb)
                        .IsDefault()
                        .LifestyleSingleton());

                _container.Kernel.AddHandlerSelector(
                    new KeyReplacementHandlerSelector(
                        typeof(IDocumentDb),
                        AccountManagementDomainQuerymodelsSessionInstaller.ComponentKeys.KeyForDocumentDb,
                        AccountManagementDomainQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb));
            }

            public void ResetDatabase()
            {
                _container.Resolve<InMemoryDocumentDb>(AccountManagementDomainQuerymodelsSessionInstaller.ComponentKeys.KeyForInMemoryDocumentDb).Clear();
            }
        }
    }
}
