using System;
using Composable.Data.ORM;

namespace Composable.CQRS
{
    public static class PersistenceSessionExtensions
    {
        public static IEntityFetcher AsEntityFetcher(this IPersistenceSession me)
        {
            return new PersistenceSessionEntityFetcherAdapter(me);
        }

        private class PersistenceSessionEntityFetcherAdapter : IEntityFetcher
        {
            private readonly IPersistenceSession _session;

            public PersistenceSessionEntityFetcherAdapter(IPersistenceSession session)
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