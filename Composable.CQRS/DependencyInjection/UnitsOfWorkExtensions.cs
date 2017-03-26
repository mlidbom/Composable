using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Composable.DependencyInjection.Windsor;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class PublicUnitOfWorkExtensions
    {
        public static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.Unsupported().BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Commit();
            }
            return result;
        }

        public static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.Unsupported().BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }

    }

    static class UnitOfWorkExtensions
    {
        public static ITransactionalUnitOfWork BeginTransactionalUnitOfWorkScope(this IServiceLocator @this)
        {
            var currentScope = TransactionalUnitOfWorkWindsorScopeBase.CurrentScope;
            if(currentScope == null)
            {
                return TransactionalUnitOfWorkWindsorScopeBase.CurrentScope = new TransactionalUnitOfWorkWindsorScope(@this);
            }
            return new InnerTransactionalUnitOfWorkWindsorScope(TransactionalUnitOfWorkWindsorScopeBase.CurrentScope);
        }

        abstract class TransactionalUnitOfWorkWindsorScopeBase : ITransactionalUnitOfWork
        {
            public abstract void Dispose();
            public abstract void Commit();
            public abstract bool IsActive { get; }

            internal static TransactionalUnitOfWorkWindsorScopeBase CurrentScope
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
                set => CallContext.SetData("TransactionalUnitOfWorkWindsorScope_Current", value);
            }
        }

        class TransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase, IEnlistmentNotification
        {
            readonly TransactionScope _transactionScopeWeCreatedAndOwn;
            readonly IUnitOfWork _unitOfWork;
            bool _committed;

            public TransactionalUnitOfWorkWindsorScope(IServiceLocator container)
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

            public override bool IsActive => !CommitCalled && !RollBackCalled && !InDoubtCalled;

            bool CommitCalled { get; set; }
            bool RollBackCalled { get; set; }
            bool InDoubtCalled { get; set; }
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
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

        class InnerTransactionalUnitOfWorkWindsorScope : TransactionalUnitOfWorkWindsorScopeBase
        {
            readonly TransactionalUnitOfWorkWindsorScopeBase _outer;

            public InnerTransactionalUnitOfWorkWindsorScope(TransactionalUnitOfWorkWindsorScopeBase outer) => _outer = outer;

            public override void Dispose()
            { }

            public override void Commit()
            { }

            public override bool IsActive => _outer.IsActive;
        }


    }

    interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}