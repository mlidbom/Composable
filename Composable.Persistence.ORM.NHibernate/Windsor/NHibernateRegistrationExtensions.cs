using System.Collections.Generic;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.System.Configuration;
using Composable.Windsor.Testing;
using NHibernate;

namespace Composable.Persistence.ORM.NHibernate.Windsor
{
    //todo: write tests.
    public static class NHibernateRegistrationExtensions
    {
        public static IWindsorContainer RegisterSqlServerNHibernateComponents
            (this IWindsorContainer @this,
             NHibernateRegistration registration,
             string connectionStringName,
             IEnumerable<Assembly> mappingAssemblies,
             NHibernateTestingMode testingMode = NHibernateTestingMode.Collection)
        {
            @this.Register(
                Component.For<INHibernateSessionSource>()
                         .UsingFactoryMethod(
                             kernel =>
                             new SqlServerNHibernateSessionSource(kernel.Resolve<IConnectionStringProvider>().GetConnectionString(connectionStringName).ConnectionString, mappingAssemblies))
                         .Named(registration.SessionSourceName)
                         .LifestyleSingleton()
                );

            @this.Register(
                Component.For<ISession>()
                         .UsingFactoryMethod(kernel => kernel.Resolve<INHibernateSessionSource>(registration.SessionSourceName).OpenSession())
                         .Named(registration.NHibernateSessionName)
                         .LifestylePerWebRequest()
                );

            @this.Register(
                Component.For<IPersistenceSession, IQueryableEntities, IEntityPersister, IEntityFetcher, IQueryableEntityFetcher>()
                         .ImplementedBy<NHibernatePersistenceSession>()
                         .DependsOn(registration.ISession)
                         .Named(registration.SessionName)
                         .LifestylePerWebRequest()
                );

            if(testingMode == NHibernateTestingMode.InMemorySqlite)
            {
                @this.WhenTesting()
                     .ReplaceComponent(
                         componentName: registration.SessionSourceName,
                         replacement: Component.For<INHibernateSessionSource>()
                                               .UsingFactoryMethod( () => new InMemoryNHibernateSessionSource(mappingAssemblies))
                                               .LifestyleSingleton()
                    );
            }
            else
            {
                @this.WhenTesting()
                     .ReplaceComponent(
                         componentName: registration.SessionName,
                         replacement: Component.For<IPersistenceSession>()
                                               .ImplementedBy<Testing.InMemoryPersistenceSession>()
                                               .LifestylePerWebRequest()
                    );
            }

            return @this;
        }
    }
}
