using NHibernate.ByteCode.LinFu;
using Void.Data.ORM.NHibernate;
using Void.Data.ORM.NHibernateRepositories.Tests.Domain;
using Void.Data.ORM.Repositories.Tests.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests.Linq
{
    public class SimpleQueries : Repositories.Tests.Linq.SimpleQueries
    {
        protected override IPersistanceSession GetPersistanceSession()
        {
            return NhibernateRepositoryTest.GetPersistanceSession();
        }

        protected override TypeWithGeneratedId GetInstance()
        {
            return new TypewithGeneratedId();
        }   
    }
}