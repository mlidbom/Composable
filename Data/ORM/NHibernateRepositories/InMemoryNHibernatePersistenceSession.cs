#region usings

using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Tool.hbm2ddl;

#endregion

namespace Composable.Data.ORM.NHibernate
{
    public class InMemoryNHibernatePersistenceSession<TProxyFactory> : NHibernatePersistenceSession
    {
        public InMemoryNHibernatePersistenceSession() : base(CreateDataBaseAndOpenSession())
        {
        }

        static InMemoryNHibernatePersistenceSession()
        {
            Configuration = new Configuration()
                .SetProperty(Environment.ReleaseConnections, "on_close")
                .SetProperty(Environment.Dialect, typeof(SQLiteDialect).AssemblyQualifiedName)
                .SetProperty(Environment.ConnectionDriver, typeof(SQLite20Driver).AssemblyQualifiedName)
                .SetProperty(Environment.ConnectionString, "data source=:memory:")
                .SetProperty(Environment.ProxyFactoryFactoryClass, typeof(TProxyFactory).AssemblyQualifiedName)
                .SetProperty(Environment.ShowSql, "true");
        }

        private static readonly Configuration Configuration;
        private static ISessionFactory _sessionFactory;

        public static void RegisterAssembly(Assembly assembly)
        {
            Configuration.AddAssembly(assembly);
            _sessionFactory = Configuration.BuildSessionFactory();
        }

        private static ISession CreateDataBaseAndOpenSession()
        {
            var session = _sessionFactory.OpenSession();
            new SchemaExport(Configuration).Execute(false, true, false, session.Connection, null);
            return session;
        }
    }
}