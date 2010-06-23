#region usings

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NHibernate;
using NHibernate.Cfg;

#endregion

namespace Void.Data.ORM.NHibernate
{
    public class ConfiguredNHibernatePersistenceSession : NHibernatePersistenceSession
    {
        public ConfiguredNHibernatePersistenceSession()
        {
        }

        public ConfiguredNHibernatePersistenceSession(IInterceptor interceptor) : base(interceptor)
        {
        }


        static ConfiguredNHibernatePersistenceSession()
        {
            //There is some odd problem where NHibernate sometimes 
            //escalates transactions unneccessarily to distributed when using the very first session that is created.
            //this code creates a session that is never used, and the problem is never encountered. 
            using (var workAround = new ConfiguredNHibernatePersistenceSession())
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