using System.Diagnostics.Contracts;
using Castle.MicroKernel.Registration;
using Composable.System;
using NHibernate;

namespace Composable.Persistence.ORM.NHibernate.Windsor
{
    public class NHibernateRegistration
    {
        protected NHibernateRegistration(string uniqueNameOfRegistration)
        {
            Contract.Requires(!uniqueNameOfRegistration.IsNullOrWhiteSpace());
            SessionSourceName = $"{uniqueNameOfRegistration}.SessionSource";
            NHibernateSessionName = $"{uniqueNameOfRegistration}.NHibernateSession";
            SessionName = $"{uniqueNameOfRegistration}.Session";
        }

        // ReSharper disable InconsistentNaming
        public Dependency IPersistenceSession => Dependency.OnComponent(typeof(IPersistenceSession), componentName: SessionName);
        public Dependency IEntityPersister => Dependency.OnComponent(typeof(IEntityPersister), componentName: SessionName);
        public Dependency IEntityFetcher => Dependency.OnComponent(typeof(IEntityFetcher), componentName: SessionName);
        public Dependency IQueryableEntityFetcher => Dependency.OnComponent(typeof(IQueryableEntityFetcher), componentName: SessionName);
        public Dependency IQueryableEntities => Dependency.OnComponent(typeof(IQueryableEntities), componentName: SessionName);
        public Dependency ISession => Dependency.OnComponent(typeof(ISession), componentName: NHibernateSessionName);

        // ReSharper restore InconsistentNaming

        internal string SessionSourceName { get; }
        internal string NHibernateSessionName { get; }
        internal string SessionName { get; }
    }

    public class NHibernateRegistration<TClient> : NHibernateRegistration
    {
        public NHibernateRegistration() : base(typeof(TClient).FullName) { }
    }
}