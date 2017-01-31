using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Castle.Windsor;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;

namespace Composable.Windsor
{
    public static class WindsorUnitOfWorkExtensions1
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

        static TransactionalUnitOfWorkWindsorScopeBase CurrentScope
        {
            get
            {
                var result = (TransactionalUnitOfWorkWindsorScopeBase)CallContext.GetData("TransactionalUnitOfWorkWindsorScope_Current");
                if (result != null && result.IsActive)
                {
                    return result;   
                }
                return CurrentScope = null;
            }
            set { CallContext.SetData("TransactionalUnitOfWorkWindsorScope_Current", value); }
        }

        private abstract class TransactionalUnitOfWorkWindsorScopeBase : ITransactionalUnitOfWork
        {
            public abstract void Dispose();
            public abstract void Commit();
            public abstract bool IsActive { get; }
        }

        private class TransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase, IEnlistmentNotification
        {
            private readonly TransactionScope _transactionScopeWeCreatedAndOwn;
            private readonly IUnitOfWork _unitOfWork;
            private bool _committed;

            public TransactionalUnitOfWorkWindsorScope(IWindsorContainer container)
            {
                _transactionScopeWeCreatedAndOwn = new TransactionScope();
                try
                {
                    _unitOfWork = new UnitOfWork(container.Resolve<ISingleContextUseGuard>());
                    _unitOfWork.AddParticipants(container.ResolveAll<IUnitOfWorkParticipant>());
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                }
                catch(Exception)
                {
                    _transactionScopeWeCreatedAndOwn.Dispose();//Under no circumstances leave transactions scopes hanging around unmanaged!
                    throw;
                }                
            }

            override public void Dispose()
            {
                CurrentScope = null;
                if(!_committed)
                {
                    _unitOfWork.Rollback();
                }
                _transactionScopeWeCreatedAndOwn.Dispose();
            }

            override public void Commit()
            {
                _unitOfWork.Commit();
                _transactionScopeWeCreatedAndOwn.Complete();
                _committed = true;
            }

            override public bool IsActive {get { return !CommitCalled && !RollBackCalled && !InDoubtCalled; }}

            public bool PrepareCalled { get; private set; }
            public bool CommitCalled { get; private set; }
            public bool RollBackCalled { get; private set; }
            public bool InDoubtCalled { get; private set; }
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                PrepareCalled = true;
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                CommitCalled = true;
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                RollBackCalled = true;
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                InDoubtCalled = true;
                enlistment.Done();
            }
        }


        private class InnerTransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase, ITransactionalUnitOfWork
        {
            private readonly TransactionalUnitOfWorkWindsorScopeBase _outer;

            public InnerTransactionalUnitOfWorkWindsorScope(TransactionalUnitOfWorkWindsorScopeBase outer)
            {
                _outer = outer;
            }

            override public void Dispose()
            { }

            override public void Commit()
            { }

            override public bool IsActive { get { return _outer.IsActive; } }
        }


    }


    public interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}
