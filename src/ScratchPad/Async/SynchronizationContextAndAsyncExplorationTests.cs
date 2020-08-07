using System;
using System.Threading;
using System.Threading.Tasks;
using Composable.SystemCE.ThreadingCE.TasksCE;
using FluentAssertions;
using Nito.AsyncEx;
using NUnit.Framework;

namespace ScratchPad.Async
{
    [TestFixture]
    public class SynchronizationContextAndAsyncExplorationTests
    {
        [SetUp] public void SetupTask()
        {
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
