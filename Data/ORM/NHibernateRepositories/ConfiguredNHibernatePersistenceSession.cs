#region usings

using System.Collections.Generic;
using NHibernate;
using NHibernate.Cfg;

#endregion

namespace Composable.Data.ORM.NHibernate
{
    public class ConfiguredNHibernatePersistenceSession : NHibernatePersistenceSession
    {
        private readonly string _configurationFile;
        public ConfiguredNHibernatePersistenceSession(string configurationFile)
        {
            _configurationFile = configurationFile;
            if (!Configurations.TryGetValue(_configurationFile, out _configuration))
            {
                lock (Configurations)
                {
                    if (!Configurations.TryGetValue(_configurationFile, out _configuration))
                    {
                        _configuration = new Configuration().Configure(_configurationFile);
                        Configurations[_configurationFile] = _configuration;
                    }
                }
            }

            if (!SessionFactories.TryGetValue(_configurationFile, out _sessionFactory))
            {
                lock (SessionFactories)
                {
                    if (!SessionFactories.TryGetValue(_configurationFile, out _sessionFactory))
                    {
                        _sessionFactory = _configuration.BuildSessionFactory();
                        SessionFactories[_configurationFile] = _sessionFactory;
                    }
                }
            }
        }

        public ConfiguredNHibernatePersistenceSession(): this("hibernate.cfg.xml")
        {
        }

        public ConfiguredNHibernatePersistenceSession(IInterceptor interceptor) : base(interceptor)
        {
        }

       
        private readonly Configuration _configuration;
        protected override Configuration Configuration { get { return _configuration; } }

        private readonly ISessionFactory _sessionFactory;
        protected override ISessionFactory SessionFactory { get { return _sessionFactory; } }

        


        private static readonly IDictionary<string, Configuration> Configurations = new Dictionary<string, Configuration>();
        private static readonly IDictionary<string, ISessionFactory> SessionFactories = new Dictionary<string, ISessionFactory>();
        
    }
}