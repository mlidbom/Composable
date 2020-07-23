using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using NUnit.Framework;

namespace Composable.Tests.System.Threading
{
    [TestFixture] public class AsyncCancellationTokenSourceTests
    {
        AsyncCancellationTokenSource _tokenSource;

        [SetUp] public void SetupTask() => _tokenSource = new AsyncCancellationTokenSource();

        [TearDown] public void TearDownTask() => _tokenSource.Dispose();

        [Test] public void WithNoCallbacksRegisteredCancelIsInvokedSynchronously()
        {
            _tokenSource.CancelAsync();
            //It seems unlikely that a thread has been spawned and changed the value between this thread executing the previous line and this line
            Assert.True(_tokenSource.Token.IsCancellationRequested);
        }

        [Test] public void WithCallbacksCancelIsInvokedAsynchronously()
        {
            _tokenSource.Token.Register(() => Thread.Sleep(100));
            var now = DateTime.UtcNow;
            _tokenSource.CancelAsync();
            Assert.LessOrEqual((DateTime.UtcNow - now),
                               TimeSpan.FromMilliseconds(50),
                               "If we syncronously wait for the sleep in the registered callback we should not get here for at least 1000 milliseconds");
        }

        //This test verifies that the class does not perform as optimally as might be wished for. If it starts failing we should be happy :)
        [Test] public void IfCallbacksHaveBeenRegisteredAndRemovedCancelIsStillInvokedAsynchronously()
        {
            var registration = _tokenSource.Token.Register(() => {});
            registration.Unregister();
            registration.Dispose();
            _tokenSource.CancelAsync();
            //It seems unlikely that a thread has been spawned and changed the value between this thread executing the previous line and this line
            Assert.False(_tokenSource.Token.IsCancellationRequested);
        }
    }
}
