#region usings

using Composable.Data.ORM.NHibernate;
using Composable.Data.ORM.NHibernateRepositories.Tests.Domain;
using NHibernate.ByteCode.Castle;

#endregion

namespace Composable.Data.ORM.NHibernateRepositories.Tests
{
    public class NhibernateRepositoryTest
    {
        public static void Init()
        {
        }

        static NhibernateRepositoryTest()
        {
            //HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
            InMemoryNHibernatePersistenceSession<ProxyFactoryFactory>.RegisterAssembly(typeof(TypewithGeneratedId).Assembly);
        }

        public static IPersistenceSession GetPersistanceSession()
        {
            return new InMemoryNHibernatePersistenceSession<ProxyFactoryFactory>();
        }
    }
}