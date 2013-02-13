using System;
using System.Diagnostics;
using System.Threading;
using Composable.System;

namespace Composable.KeyValueStorage.Population
{
    public class ConsolePopulationBatchLogger : IPopulationBatchLogger
    {
        private int _consoleStartPositionLeft;
        private int _consoleStartPositionTop;

        readonly object _lockObject = new object();
        int _handled = 0;
        private const int LogEach = 1000;
        readonly Stopwatch _batchStopWatch = new Stopwatch();
        readonly Stopwatch _totalStopWatch = new Stopwatch();
        private int _numberOfAggregates;


        public void Initialize(int numberOfAggregates)
        {
            _handled = 0;
            _consoleStartPositionLeft = Console.CursorLeft;
            _consoleStartPositionTop = Console.CursorTop;
            _batchStopWatch.Reset();
            _totalStopWatch.Reset();
            _batchStopWatch.Start();
            _totalStopWatch.Start();
            _numberOfAggregates = numberOfAggregates;
        }


        public void LogAggregateHandled(Guid ignored)
        {
            var handledCurrent = Interlocked.Increment(ref _handled);
            if (handledCurrent % LogEach == 0 || handledCurrent == _numberOfAggregates)
            {
                DisplayProgress(handledCurrent);
            }
        }

        private void DisplayProgress(int handledCurrent)
        {
            lock(_lockObject)
            {
                Console.SetCursorPosition(_consoleStartPositionLeft, _consoleStartPositionTop);
                Console.WriteLine(@"

Done:                {0}%                             
Total Entities:      {1}                              
Handled entities:    {2}                           
Current speed:       {3:F0}/s                   
Average speed:       {4:F0}/s                  
Elapsed Time:        {5}                      
Estimated time left: {6}                     
",
                                  (int)(((double)handledCurrent/_numberOfAggregates)*100),
                                  _numberOfAggregates,
                                  handledCurrent,
                                  LogEach/_batchStopWatch.Elapsed.TotalSeconds,
                                  handledCurrent/_totalStopWatch.Elapsed.TotalSeconds,
                                  ((int)_totalStopWatch.Elapsed.TotalSeconds).Seconds(),
                                  ((int)((_numberOfAggregates - handledCurrent)/(handledCurrent/_totalStopWatch.Elapsed.TotalSeconds))).Seconds());

                _batchStopWatch.Reset();
                _batchStopWatch.Start();
            }
        }

        public void LogError(Exception e, Guid entityId)
        {
            var handledCurrent = Interlocked.Increment(ref _handled);
            lock (_lockObject)
            {
                Console.Write("ERROR: Failed to repopulate viewmodels for aggregate: {0} ", entityId);
                Console.WriteLine(e);

                _consoleStartPositionLeft = Console.CursorLeft;
                _consoleStartPositionTop = Console.CursorTop;
            }
            DisplayProgress(handledCurrent);
        }
    }
}