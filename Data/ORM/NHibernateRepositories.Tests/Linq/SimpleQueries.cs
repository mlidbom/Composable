using NHibernate.ByteCode.Castle;
using Void.Data.ORM.NHibernate;
using Void.Data.ORM.NHibernateRepositories.Tests.Domain;
using Void.Data.ORM.Repositories.Tests.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests.Linq
{
    public class SimpleQueries : Repositories.Tests.Linq.SimpleQueries
    {
        protected override IPersistenceSession GetPersistanceSession()
        {
            return NhibernateRepositoryTest.GetPersistanceSession();
        }

        protected override TypeWithGeneratedId GetInstance()
        {
            return new TypewithGeneratedId();
        }   
    }
}