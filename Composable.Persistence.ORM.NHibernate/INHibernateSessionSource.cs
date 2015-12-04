using NHibernate;

namespace Composable.Persistence.ORM.NHibernate
{
    public interface INHibernateSessionSource
    {
        ISession OpenSession();
    }
}