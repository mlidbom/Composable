using NHibernate.ByteCode.LinFu;
using Void.Data.ORM.NHibernate;
using Void.Data.ORM.NHibernateRepositories.Tests.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests
{
    public class NhibernateRepositoryTest
    {
        public static void Init()
        {
            
        }

        static NhibernateRepositoryTest()
        {            
            InMemoryNHibernatePersistanceSession<ProxyFactoryFactory>.RegisterAssembly(typeof(TypewithGeneratedId).Assembly);
        }
    }
}