using System;
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
            HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
            InMemoryNHibernatePersistanceSession<ProxyFactoryFactory>.RegisterAssembly(typeof(TypewithGeneratedId).Assembly);
        }

        public static IPersistanceSession GetPersistanceSession()
        {
            return new InMemoryNHibernatePersistanceSession<ProxyFactoryFactory>();
        }
    }
}