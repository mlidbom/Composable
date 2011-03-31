#region usings

using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

#endregion

namespace Composable.Data.ORM.NHibernate
{
    public static class SessionExtensions
    {
        public static void SchemaExport(this Configuration me, ISession session)
        {
            new SchemaExport(me).Execute(false, true, false, session.Connection, null);
        }
    }
}