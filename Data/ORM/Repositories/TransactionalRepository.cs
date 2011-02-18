using System;
using System.Collections.Generic;
using System.Transactions;

namespace Composable.Data.ORM
{
    /// <summary>
    /// Extends the base repository by wrapping all modifying methods in a <see cref="TransactionScope"/>.
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class TransactionalRepository<TInstance, TKey> : Repository<TInstance, TKey>
    {
        public TransactionalRepository(IPersistenceSession session) : base(session)
        {
        }

        public override void SaveOrUpdate(TInstance instance)
        {
            using(var transaction = new TransactionScope())
            {
                base.SaveOrUpdate(instance);
                transaction.Complete();
            }
        }

        public override void SaveOrUpdate(IEnumerable<TInstance> instances)
        {
            using (var transaction = new TransactionScope())
            {
                base.SaveOrUpdate(instances);
                transaction.Complete();
            }
        }

        public override void Delete(TInstance instance)
        {
            using (var transaction = new TransactionScope())
            {
                base.Delete(instance);
                transaction.Complete();
            }
        }
    }
}