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
}
