using System;
using NHibernate.ByteCode.LinFu;
using NUnit.Framework;
using Void.Data.ORM.NHibernate;
using Void.Data.ORM.NHibernateRepositories.Tests.Domain;
using Void.Data.ORM.Repositories.Tests.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests.WhenFirstSaving
{
    [TestFixture]
    public class WithGeneratedId : Repositories.Tests.WhenFirstSavingInstance.WithGeneratedId
    {
        static WithGeneratedId()
        {
            NhibernateRepositoryTest.Init();
        }

        protected override IPersistanceSession GetPersistanceSession()
        {            
            //InMemoryNHibernatePersistanceSession<ProxyFactoryFactory>.RegisterMappingFile(@"WhenFirstSaving\TypeWithGeneratedId.hbm.xml");
            return new InMemoryNHibernatePersistanceSession<ProxyFactoryFactory>();
        }

        protected override TypeWithGeneratedId GetInstance()
        {
            return new TypewithGeneratedId();
        }
    }
}