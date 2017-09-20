using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using Composable.Testing.Threading;
using FluentAssertions;

namespace Composable.Tests.Testing.Threading
{
    class ThreadGateTestFixture : IDisposable
    {
        public readonly IThreadGate Gate;
        public int NumberOfThreads { get; private set; }
        public IReadOnlyList<Entrant> EntrantEvents;
        Task[] _tasksPassingGate;

        public class Entrant
        {
            public ManualResetEventSlim HasStarted { get; set; }
            public ManualResetEventSlim HasCompleted { get; set; }
        }

        public static ThreadGateTestFixture StartEntrantsOnThreads(int threadCount)
        {
            var fixture = new ThreadGateTestFixture
                          {
                              NumberOfThreads = threadCount
                          };
            fixture.StartThreads();
            return fixture;
        }

        ThreadGateTestFixture()
        {
            Gate = ThreadGate.CreateClosedWithTimeout(1.Seconds());
            NumberOfThreads = 10;
        }

        void StartThreads()
        {
            EntrantEvents = 1.Through(NumberOfThreads)
                             .Select(
                                 _ => new Entrant()
                                      {
                                          HasStarted = new ManualResetEventSlim(),
                                          HasCompleted = new ManualResetEventSlim()
                                      })
                             .ToList();

            _tasksPassingGate = EntrantEvents.Select(
                                                 entrantEvent => Task.Factory.StartNew(
                                                  () =>
                                                  {
                                                      entrantEvent.HasStarted.Set();
                                                      Gate.AwaitPassthrough();
                                                      entrantEvent.HasCompleted.Set();
                                                  },
                                                  TaskCreationOptions.LongRunning))
                                          .ToArray();
        }

        public int ThreadsPassedTheGate(TimeSpan waitTime)
        {
            Thread.Sleep(waitTime);
            return EntrantEvents.Count(entrant => entrant.HasCompleted.IsSet);
        }

        public ThreadGateTestFixture WaitForAllThreadsToQueueUpAtPassThrough()
        {
            Gate.Await(() => Gate.Queued == NumberOfThreads);
            return this;
        }

        public void Dispose()
        {
            Gate.Open();
            Task.WaitAll(_tasksPassingGate);
        }
    }
}
