using System;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE;
using Composable.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using Nito.AsyncEx;
using NUnit.Framework;

namespace ScratchPad.Async
{
    [TestFixture]
    public class SynchronizationContextAndAsyncExplorationTests
    {
        [Test] public void Exceptions_in_async_methods_are_always_marshaled_to_the_task_result()
        {
            var result = Throw();

            Assert.Throws<Exception>(() => result.SyncResult());
        }

        [Test] public async Task Async_Exceptions_in_async_methods_are_always_marshaled_to_the_task_result()
        {
            var result = Throw();

            var thrown = false;
            try
            {
                await result;
            }
            catch(Exception)
            {
                thrown = true;
            }

            thrown.Should().BeTrue();
        }

        static async Task Throw(bool @throw = true)
        {
            if(@throw)
            {
                throw new Exception();
            }

            await Task.CompletedTask;
        }

        [Test] public void PrintContextOther()
        {
            AsyncContext.Run(async () =>
            {

                SynchronizationContext.Current.Should().NotBe(null);
                //await Task.Delay(1).NoMarshalling();
                //PrintContextInformation("After task delay");
                PrintContextInformation("Before 1st top level async call");
                Level1Async().Wait();
                PrintContextInformation("After 1st top level async call");
                //await Level1Async().ConfigureAwait(false);

                PrintContextInformation("Before 2nd top level async call");
                await Level1Async();
                PrintContextInformation("After 2nd top level async call");

                SynchronizationContext.Current.Should().NotBe(null);
            });
        }

        static async Task Level1Async()
        {
            PrintContextInformation("Level1Async start");
            await Level2Async().NoMarshalling();
            PrintContextInformation("Level1Async end");
        }

        static async Task Level2Async()
        {
            PrintContextInformation("Level2Async start");
            await Task.Delay(1).NoMarshalling();
            PrintContextInformation("Level2Async end");
        }

        static void PrintContextInformation(string context) { Console.WriteLine($"{context + ":",-20} SynchronizationContext: {SynchronizationContext.Current?.ToString() ?? "null"} CurrentThread: {Thread.CurrentThread.ManagedThreadId}"); }
    }
}
