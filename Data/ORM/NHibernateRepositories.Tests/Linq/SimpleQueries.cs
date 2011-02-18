#region usings

using Composable.Data.ORM.NHibernateRepositories.Tests.Domain;
using Composable.Data.ORM.Repositories.Tests.Domain;

#endregion

namespace Composable.Data.ORM.NHibernateRepositories.Tests.Linq
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