using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Castle.Windsor;
using Composable.CQRS.UnitsOfWork;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;

namespace Composable.CQRS.Windsor
{
    static class WindsorUnitOfWorkExtensions1
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

        abstract class TransactionalUnitOfWorkWindsorScopeBase : ITransactionalUnitOfWork
        {
            public abstract void Dispose();
            public abstract void Commit();
            public abstract bool IsActive { get; }
        }

        class TransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase, IEnlistmentNotification
        {
            readonly TransactionScope _transactionScopeWeCreatedAndOwn;
            readonly IUnitOfWork _unitOfWork;
            bool _committed;

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

            public override void Dispose()
            {
                CurrentScope = null;
                if(!_committed)
                {
                    _unitOfWork.Rollback();
                }
                _transactionScopeWeCreatedAndOwn.Dispose();
            }

            public override void Commit()
            {
                _unitOfWork.Commit();
                _transactionScopeWeCreatedAndOwn.Complete();
                _committed = true;
            }

            public override bool IsActive {get { return !CommitCalled && !RollBackCalled && !InDoubtCalled; }}

            bool PrepareCalled { get; set; }
            bool CommitCalled { get; set; }
            bool RollBackCalled { get; set; }
            bool InDoubtCalled { get; set; }
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

        class InnerTransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase, ITransactionalUnitOfWork
        {
            readonly TransactionalUnitOfWorkWindsorScopeBase _outer;

            public InnerTransactionalUnitOfWorkWindsorScope(TransactionalUnitOfWorkWindsorScopeBase outer)
            {
                _outer = outer;
            }

            public override void Dispose()
            { }

            public override void Commit()
            { }

            public override bool IsActive { get { return _outer.IsActive; } }
        }


    }

    interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}
