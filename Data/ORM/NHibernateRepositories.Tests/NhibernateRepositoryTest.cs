using System;
using NHibernate.ByteCode.Castle;
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
            //HibernatingRhinos.Profiler.Appender.NHibernate.NHibernateProfiler.Initialize();
            InMemoryNHibernatePersistenceSession<ProxyFactoryFactory>.RegisterAssembly(typeof(TypewithGeneratedId).Assembly);
        }

        public static IPersistenceSession GetPersistanceSession()
        {
            return new InMemoryNHibernatePersistenceSession<ProxyFactoryFactory>();
        }
    }
}