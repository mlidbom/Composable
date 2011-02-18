#region usings

using Composable.Data.ORM.NHibernateRepositories.Tests.Domain;
using Composable.Data.ORM.Repositories.Tests.Domain;
using NUnit.Framework;

#endregion

namespace Composable.Data.ORM.NHibernateRepositories.Tests.WhenFirstSaving
{
    [TestFixture]
    public class WithGeneratedId : Repositories.Tests.WhenFirstSaving.WithGeneratedId
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