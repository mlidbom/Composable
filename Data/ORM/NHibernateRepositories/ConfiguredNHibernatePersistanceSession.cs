using NHibernate;
using NHibernate.Cfg;

namespace Void.Data.ORM.NHibernate
{
    public class ConfiguredNHibernatePersistanceSession : NHibernatePersistanceSession
    {
        public ConfiguredNHibernatePersistanceSession()
        {
            
        }

        public ConfiguredNHibernatePersistanceSession(IInterceptor interceptor):base(interceptor)
        {
            
        }


        static ConfiguredNHibernatePersistanceSession()
        {
            //There is some odd problem where NHibernate sometimes 
            //escalates transactions unneccessarily to distributed when using the very first session that is created.
            //this code creates a session that is never used, and the problem is never encountered. 
            using (var workAround = new ConfiguredNHibernatePersistanceSession())
            {
                workAround.SessionFactory.OpenSession();
            }
        }

        private static ISessionFactory sessionFactory;

        protected override ISessionFactory SessionFactory
        {
            get
            {
                if (sessionFactory == null)
                {
                    sessionFactory = Configuration.BuildSessionFactory();
                }
                return sessionFactory;
            }
        }

        private static Configuration configuration;

        protected override Configuration Configuration
        {
            get
            {
                if (configuration == null)
                {
                    configuration = new Configuration().Configure();
                }
                return configuration;
            }
        }
    }
}