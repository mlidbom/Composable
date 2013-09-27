using System;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Castle.Windsor;
using Castle.MicroKernel.Lifestyle;
using Composable.System;
using Composable.System.Transactions;
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

        private static TransactionalUnitOfWorkWindsorScopeBase CurrentScope
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
            private readonly Transaction _ambientTransactionAfterCreation;

            public TransactionalUnitOfWorkWindsorScope(IWindsorContainer container)
            {
                _transactionScopeWeCreatedAndOwn = new TransactionScope();
                _ambientTransactionAfterCreation = Transaction.Current;
                _ambientTransactionAfterCreation.EnlistVolatile(this, EnlistmentOptions.None);
                _unitOfWork = new UnitOfWork(container.Resolve<ISingleContextUseGuard>());
                _unitOfWork.AddParticipants(container.ResolveAll<IUnitOfWorkParticipant>());
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

            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                Console.WriteLine("_ambientTransactionAfterCreation == Transaction.Current->{0}", _ambientTransactionAfterCreation != Transaction.Current);
                PrepareCalled = true;
                preparingEnlistment.Done();
            }

            override public bool IsActive {get { return !CommitCalled && !RollBackCalled && !InDoubtCalled; }}

            public bool PrepareCalled { get; private set; }
            public bool CommitCalled { get; private set; }
            public bool RollBackCalled { get; private set; }
            public bool InDoubtCalled { get; private set; }

            void IEnlistmentNotification.Commit(Enlistment enlistment)
            {
                CommitCalled = true;
                enlistment.Done();
            }

            void IEnlistmentNotification.Rollback(Enlistment enlistment)
            {
                RollBackCalled = true;
                enlistment.Done();
            }

            void IEnlistmentNotification.InDoubt(Enlistment enlistment)
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
