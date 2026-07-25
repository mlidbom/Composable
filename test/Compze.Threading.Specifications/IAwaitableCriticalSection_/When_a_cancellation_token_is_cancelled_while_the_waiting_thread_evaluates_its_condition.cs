using Compze.Internals.SystemCE;
using Compze.Must;
using Compze.Tests.Infrastructure;
using Compze.Threading.Specifications.IAwaitableCriticalSection_.Infrastructure;
using Compze.Threading.Specifications.TestInfrastructure;
using Compze.Threading.Testing;
using Xunit;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Threading.Specifications.IAwaitableCriticalSection_;

///<summary>A condition wait evaluates its condition while holding the lock, so cancellation can land while the waiting thread<br/>
/// holds it. An implementation that answers cancellation by registering a callback which takes that same lock must not then
/// wait for that callback while still holding the lock: the callback cannot run until the lock is released, and the lock is
/// not released until the callback returns.</summary>
///<remarks>The existing cancellation specifications all cancel a thread parked in the wait itself, which releases the lock -
/// so the callback runs, the wait wakes, and nothing deadlocks. This one pins the window they leave open: cancellation
/// arriving while the condition is being evaluated, with the lock held.</remarks>
[Collection(nameof(NonParallelCollection))]
public class When_a_cancellation_token_is_cancelled_while_the_waiting_thread_evaluates_its_condition : UniversalTestBase
{
   readonly IAwaitableCriticalSectionMatrixAttribute.Factory<When_a_cancellation_token_is_cancelled_while_the_waiting_thread_evaluates_its_condition> _factory = new();

   protected override void DisposeInternal() => _factory.Dispose();

   [IAwaitableCriticalSectionMatrix]
   public void the_wait_completes_instead_of_deadlocking_against_the_cancellation_callback()
   {
      var (waitCompleted, waitFailure) = CancelAWaitWhoseConditionIsBeingEvaluated(WaitTimeout.Seconds(10));

      waitFailure.Must().BeNull();
      waitCompleted.Must().BeTrue();
   }

   (bool WaitCompleted, Exception? WaitFailure) CancelAWaitWhoseConditionIsBeingEvaluated(WaitTimeout patience)
   {
      var criticalSection = _factory.Create(LockTimeout.Seconds(30));
      using var cancellation = new CancellationTokenSource();
      var cancellationRequested = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "cancellationRequested");
      var waitCompleted = IThreadGate.NewOpen(patience, "waitCompleted");
      Exception? waitFailure = null;

      var cancellingThread = new Thread(RequestCancellation) { IsBackground = true, Name = nameof(RequestCancellation) };
      new Thread(AwaitTheCondition) { IsBackground = true, Name = nameof(AwaitTheCondition) }.Start();

      return (waitCompleted.TryAwaitPassedThroughCountEqualTo(1, patience), waitFailure);

      void AwaitTheCondition()
      {
         try
         {
            criticalSection.TryAwait(RequestCancellationFromTheOtherThreadAndThenHold, cancellation.Token, WaitTimeout.Seconds(30));
         }
#pragma warning disable CA1031 //Whatever the wait throws must fail this specification. An escaping exception on a background thread kills the test host instead.
         catch(Exception exception)
         {
#pragma warning restore CA1031
            waitFailure = exception;
         }

         waitCompleted.AwaitPassThrough();
      }

      //Runs on the waiting thread, which holds the lock for every evaluation of the condition. Holding - returning true - ends
      //the wait, so the wait leaves through the disposal of its cancellation registration with the lock still held, which is
      //the window this specification pins.
      bool RequestCancellationFromTheOtherThreadAndThenHold()
      {
         cancellingThread.Start();
         cancellationRequested.AwaitPassedThroughCountEqualTo(1);
         AwaitTheCancellingThreadBlockingOrFinishing();
         return true;
      }

      void RequestCancellation()
      {
         cancellationRequested.AwaitPassThrough();
         cancellation.Cancel();
      }

      //An implementation whose cancellation callback takes the lock leaves this thread blocked inside Cancel; one that polls
      //the token instead runs Cancel to completion and the thread finishes. Either outcome means the race is set up, so wait
      //for whichever arrives and then let it settle - reaching a blocking call is not the same instant as being blocked in it.
      //Losing this race can only make the specification pass without exercising the window, never fail a sound implementation.
      void AwaitTheCancellingThreadBlockingOrFinishing()
      {
         var deadline = DateTime.UtcNow + 2.Seconds();
         while(DateTime.UtcNow < deadline
            && cancellingThread.IsAlive
            && !cancellingThread.ThreadState.HasFlag(System.Threading.ThreadState.WaitSleepJoin))
            Thread.Yield();

         Thread.Sleep(50.Milliseconds());
      }
   }
}
