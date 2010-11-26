using System;
using Composable.Data.ORM;

namespace Composable.CQRS
{
    public static class PersistenceSessionExtensions
    {
        public static IEntityFetcher AsEntityFetcher(this IPersistenceSession me)
        {
            return new PersistanceSessionEntityFetcherAdapter(me);
        }

        private class PersistanceSessionEntityFetcherAdapter : IEntityFetcher
        {
            private readonly IPersistenceSession _session;

            public PersistanceSessionEntityFetcherAdapter(IPersistenceSession session)
            {
                _session = session;
            }

            public TEntity Get<TEntity>(object entityId)
            {
                return _session.Get<TEntity>(entityId);
            }
        }
    }
}