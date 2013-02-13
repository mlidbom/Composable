using System;
using System.Transactions;
using Castle.Windsor;
using Castle.MicroKernel.Lifestyle;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;

namespace Composable.KeyValueStorage.Population
{
    public static class WindsorUnitOfWorkExtensions
    {
        public static ITransactionalUnitOfWork BeginTransactionalUnitOfWorkScope(this IWindsorContainer me)
        {
            return new TransactionalUnitOfWorkWindsorScope(me);
        }

        private class TransactionalUnitOfWorkWindsorScope : ITransactionalUnitOfWork
        {
            private readonly TransactionScope _transaction;
            private readonly IUnitOfWork _unitOfWork;
            private readonly IDisposable _windsorScope;
            private bool _committed = false;

            public TransactionalUnitOfWorkWindsorScope(IWindsorContainer container)
            {
                _windsorScope = container.BeginScope();
                _transaction = CreateTransactionScope();
                _unitOfWork = new UnitOfWork(container.Resolve<ISingleContextUseGuard>());
                _unitOfWork.AddParticipants(container.ResolveAll<IUnitOfWorkParticipant>());
            }

            /// <summary>
            /// Return a default TransActionScope with correct parameters
            /// </summary>
            /// <returns>TransactionScope</returns>
            /// <remarks>See http://blogs.msdn.com/b/dbrowne/archive/2010/06/03/using-new-transactionscope-considered-harmful.aspx </remarks>
            private static TransactionScope CreateTransactionScope()
            {
                var transactionOptions = new TransactionOptions
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TransactionManager.MaximumTimeout
                };
                return new TransactionScope(TransactionScopeOption.Required, transactionOptions);
            }

            public void Dispose()
            {
                if(!_committed)
                {
                    _unitOfWork.Rollback();
                }
                _transaction.Dispose();
                _windsorScope.Dispose();
            }

            public void Commit()
            {
                _unitOfWork.Commit();
                _transaction.Complete();
                _committed = true;
            }
        }
    }


    public interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}
