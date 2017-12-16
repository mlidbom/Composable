using System;
using System.Threading;
using System.Transactions;
using Composable.System.Transactions;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.TransactionsApiExploration
{
    [TestFixture] public class TransactionOnCompletedEventTests
    {
        [Test] public void TransactionCompleted_Runs_on_same_thread_as_transaction()
        {
            Thread onCompletedThread = null;
            TransactionScopeCe.Execute(() => Transaction.Current.TransactionCompleted += (_, __) => onCompletedThread = Thread.CurrentThread);

            onCompletedThread.Should().Be(Thread.CurrentThread);
        }

        [Test] public void Exception_thrown_in_TransactionCompleted_event_handler_does_not_fail_transaction()
        {
            TransactionInterceptor interceptor = null;
            AssertThrows.Exception<Exception>(() => TransactionScopeCe.Execute(() =>
            {
                interceptor = Transaction.Current.Intercept();
                Transaction.Current.TransactionCompleted += (_, __) => throw new Exception();
            }));

            interceptor.CommittedSuccessfully.Should().Be(true);
        }

        [Test] public void ForceRollback_called_in_participant_Prepare_fails_transaction()
        {
            TransactionInterceptor saboteurInterceptor = null, interceptor = null;
            var exception = AssertThrows.Exception<TransactionAbortedException>(() => TransactionScopeCe.Execute(() =>
            {
                interceptor = Transaction.Current.Intercept();
                saboteurInterceptor = Transaction.Current.Intercept(onPrepare: _ => _.ForceRollback(new Exception("Hi")));
            }));

            exception.InnerException.Message.Should().Be("Hi");

            interceptor.RollbackCalled.Should().Be(true);
            interceptor.PrepareCalled.Should().Be(true);
            saboteurInterceptor.RollbackCalled.Should().Be(false);

            interceptor.Status.Should().Be(TransactionStatus.Aborted);
            saboteurInterceptor.Status.Should().Be(TransactionStatus.Aborted);
        }
    }

    static class TransactionInterceptorExtensions
    {
        public static void FailOnPrepare(this Transaction @this, Exception exception = null) =>
            @this.Intercept(onPrepare: enlistment => enlistment.ForceRollback(exception ?? new Exception($"{nameof(TransactionInterceptorExtensions)}.{nameof(FailOnPrepare)}")));

        public static TransactionInterceptor Intercept(this Transaction @this,
                                               Action<PreparingEnlistment> onPrepare = null,
                                               Action<Enlistment> onCommit = null,
                                               Action<Enlistment> onRollback = null,
                                               Action<Enlistment> onInDoubt = null)
            => new TransactionInterceptor(@this, onPrepare, onCommit, onRollback, onInDoubt);
    }

    class TransactionInterceptor : IEnlistmentNotification
    {
        readonly Transaction _transaction;
        readonly Action<PreparingEnlistment> _onPrepare;
        readonly Action<Enlistment> _onCommit;
        readonly Action<Enlistment> _onRollback;
        readonly Action<Enlistment> _onInDoubt;

        public TransactionInterceptor(Transaction transaction,
                              Action<PreparingEnlistment> onPrepare = null,
                              Action<Enlistment> onCommit = null,
                              Action<Enlistment> onRollback = null,
                              Action<Enlistment> onInDoubt = null)
        {
            _transaction = transaction;
            _onPrepare = onPrepare;
            _onCommit = onCommit;
            _onRollback = onRollback;
            _onInDoubt = onInDoubt;
            transaction.EnlistVolatile(this, EnlistmentOptions.None);
            transaction.TransactionCompleted += (sender, args) => CompletedInformation = args.Transaction.TransactionInformation;
        }

        public TransactionInformation CompletedInformation { get; private set; }

        public TransactionStatus Status => CompletedInformation?.Status ?? _transaction.TransactionInformation.Status;

        public bool CommittedSuccessfully => Status == TransactionStatus.Committed && PrepareCalled && CommitCalled && !RollbackCalled && !InDoubtCalled;

        public bool PrepareCalled { get; private set; }
        void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
        {
            PrepareCalled = true;

            if(_onPrepare != null)
            {
                _onPrepare(preparingEnlistment);
            } else
            {
                preparingEnlistment.Prepared();
            }
        }

        public bool CommitCalled { get; private set; }
        void IEnlistmentNotification.Commit(Enlistment enlistment)
        {
            CommitCalled = true;

            if(_onCommit != null)
            {
                _onCommit.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }

        public bool RollbackCalled { get; private set; }
        void IEnlistmentNotification.Rollback(Enlistment enlistment)
        {
            RollbackCalled = true;

            if(_onRollback != null)
            {
                _onRollback.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }

        public bool InDoubtCalled { get; private set; }
        void IEnlistmentNotification.InDoubt(Enlistment enlistment)
        {
            InDoubtCalled = true;

            if(_onInDoubt != null)
            {
                _onInDoubt.Invoke(enlistment);
            } else
            {
                enlistment.Done();
            }
        }
    }
}
