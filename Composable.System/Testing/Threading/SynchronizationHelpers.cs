using System;
using System.Threading;
using Composable.System.Threading;

namespace Composable.Testing.Threading
{
    ///<summary>A block of code with <see cref="ThreadGate"/>s for <see cref="EntranceGate"/> and <see cref="ExitGate"/>. Useful for controlling multithreaded code for testing purposes.</summary>
    class GatedCodeBlock
    {
        public ThreadGate EntranceGate { get; }
        public ThreadGate ExitGate { get; }

        readonly Action _codeBlock;

        GatedCodeBlock(Action codeBlock, ThreadGate entranceGate, ThreadGate exitGate)
        {
            EntranceGate = entranceGate;
            ExitGate = exitGate;
            _codeBlock = codeBlock;
        }
    }

    [Flags] enum ThreadGateOptions
    {
        LockOnNextPass = 1,
        LockOnEveryPass = 2,
        Locked = 4
    }

    ///<summary>Blocks threads calling <see cref="PassThrough"/> until the the gate is opened.</summary>
    class ThreadGate
    {
        ///<summary>Opens the gate</summary>
        public void Open() {}
        public void Close() {}

        public void PassThrough(TimeSpan timeout)
        {
        }

        public string Name { get; }
        public bool LockOnNextPass { get; private set; }
        public bool LockEveryPass { get; private set; }

        ThreadGate(string name, TimeSpan timeout, ThreadGateOptions options)
        {
            _gateLock = new object();
            _settingsLock = new object();
            _timeout = timeout;
            _options = options;
            Name = name;

            if((options & ThreadGateOptions.Locked) != 0)
            {
                Close();
            }

            LockOnNextPass = (options & ThreadGateOptions.LockOnNextPass) != 0;
            LockEveryPass = (options & ThreadGateOptions.LockOnEveryPass) != 0;
        }

        readonly TimeSpan _timeout;
        readonly ThreadGateOptions _options;
        object _gateLock;
        object _settingsLock;
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
            if(!_event.WaitOne(_timeout))
            {
                throw new Exception($"Timed out waiting for lock: {_name}");
            }
        }

        public void Set() => _event.Set();
        public void Reset() => _event.Reset();
    }
}
