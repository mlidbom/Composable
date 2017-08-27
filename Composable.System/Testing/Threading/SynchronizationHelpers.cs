using System;
using System.Threading;

namespace Composable.Testing.Threading
{


    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntryGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    class GatedCodeBlock
    {
        public ThreadGate EntryGate { get; }
        public ThreadGate ExitGate { get; }


        readonly Action _codeBlock;

        GatedCodeBlock(Action codeBlock, ThreadGate entryGate, ThreadGate exitGate)
        {
            EntryGate = entryGate;
            ExitGate = exitGate;
            _codeBlock = codeBlock;
        }
    }



    enum ThreadGateOptions
    {
        LockOnNextPass,
        LockOnEveryPass
    }

    ///<summary>Blocks threads calling <see cref="BlockUntilOpen"/> until the the gate is opened.</summary>
    class ThreadGate
    {
        ///<summary>Opens the gate</summary>
        public void Open() {}
        public void Close() {}
        public void BlockUntilOpen(TimeSpan timeout) {}
        public int Passed { get; }
        public string Name { get; }
        public ThreadGateOptions Options { get; }

        ThreadGate(ThreadGateOptions options)
        {
            Options = options;
        }
    }

    class TestingManualResetEvent
    {
        readonly ManualResetEventSlim _event = new ManualResetEventSlim(false);
        readonly TimeSpan _timeout;
        readonly string _name;

        public TestingManualResetEvent(string name, TimeSpan? timeout = null)
        {
            _timeout = timeout ?? TimeSpan.FromSeconds(1);
            _name = name;
        }

        public void Wait()
        {
            if(!_event.Wait(_timeout))
            {
                throw new Exception($"Timed out waiting for lock: {_name}");
            }
        }

        public void Set() => _event.Set();
        public void Reset() => _event.Reset();
    }

    class TestingAutoResetEvent
    {
        readonly AutoResetEvent _event;
        readonly TimeSpan _timeout;
        readonly string _name;

        public TestingAutoResetEvent(string name, bool startLocked, TimeSpan? timeout = null)
        {
            _timeout = timeout ?? TimeSpan.FromSeconds(1);
            _event = new AutoResetEvent(!startLocked);
            _name = name;
        }

        public void Wait()
        {
            if (!_event.WaitOne(_timeout))
            {
                throw new Exception($"Timed out waiting for lock: {_name}");
            }
        }

        public void Set() => _event.Set();
        public void Reset() => _event.Reset();
    }
}
