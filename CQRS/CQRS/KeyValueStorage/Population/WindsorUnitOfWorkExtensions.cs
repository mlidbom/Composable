using System;
using System.Runtime.Remoting.Messaging;
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
            var currentScope = CurrentScope;
            if(currentScope == null)
            {
                return CurrentScope = new TransactionalUnitOfWorkWindsorScope(me);    
            }
            return new InnerTransactionalUnitOfWorkWindsorScope(CurrentScope);
        }

        private static ITransactionalUnitOfWork CurrentScope
        {
            get { return (ITransactionalUnitOfWork)CallContext.GetData("TransactionalUnitOfWorkWindsorScope_Current"); }
            set { CallContext.SetData("TransactionalUnitOfWorkWindsorScope_Current", value); }
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
                _transaction = new TransactionScope();
                _unitOfWork = new UnitOfWork(container.Resolve<ISingleContextUseGuard>());
                _unitOfWork.AddParticipants(container.ResolveAll<IUnitOfWorkParticipant>());
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

    public class InnerTransactionalUnitOfWorkWindsorScope : ITransactionalUnitOfWork
    {
        private readonly ITransactionalUnitOfWork _outer;

        public InnerTransactionalUnitOfWorkWindsorScope(ITransactionalUnitOfWork outer)
        {
            _outer = outer;
        }

        public void Dispose()
        {}

        public void Commit()
        {}
    }


    public interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}
