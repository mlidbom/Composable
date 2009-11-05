using System;
using System.Reflection;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using Environment = NHibernate.Cfg.Environment;

namespace Void.Data.ORM.NHibernate
{
    public class InMemoryNHibernatePersistanceSession<TProxyFactory> : NHibernatePersistanceSession
    {
        public InMemoryNHibernatePersistanceSession(IInterceptor interceptor):base(interceptor)
        {
            CreateDataBase();
        }

        public InMemoryNHibernatePersistanceSession()
        {
            CreateDataBase();
        }

        static InMemoryNHibernatePersistanceSession()
        {
            _configuration = new Configuration()
                .SetProperty(Environment.ReleaseConnections, "on_close")
                .SetProperty(Environment.Dialect, typeof (SQLiteDialect).AssemblyQualifiedName)
                .SetProperty(Environment.ConnectionDriver, typeof (SQLite20Driver).AssemblyQualifiedName)
                .SetProperty(Environment.ConnectionString, "data source=:memory:")
                .SetProperty(Environment.ProxyFactoryFactoryClass, typeof (TProxyFactory).AssemblyQualifiedName)
                .SetProperty(Environment.ShowSql, "true");
        }

        public static void RegisterAssembly(Assembly assembly)
        {
            _configuration.AddAssembly(assembly);
            _sessionFactory = _configuration.BuildSessionFactory();
        }

        private static ISessionFactory _sessionFactory;
        protected override ISessionFactory SessionFactory
        {
            get
            {
                if (_sessionFactory == null)
                {
                    _sessionFactory = Configuration.BuildSessionFactory();
                }
                return _sessionFactory;
            }
        }

        private static readonly Configuration _configuration;
        protected override Configuration Configuration { get { return _configuration; } }

        public static void RegisterMappingFile(string mappingFile)
        {
            _configuration.AddFile(mappingFile);
        }
    }
}