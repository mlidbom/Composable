using System;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;

namespace Composable.CQRS.Windsor
{
    public static class WindsorUnitOfWorkExtensions
    {
        public static void ExecuteUnitOfWork(this IWindsorContainer me, Action action)
        {
            using(var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }
    }
}
