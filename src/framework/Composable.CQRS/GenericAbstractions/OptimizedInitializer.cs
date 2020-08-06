using System;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.GenericAbstractions
{
    public class OptimizedInitializer
    {
        readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
        bool _initialized;
        readonly Action _initialize;

        internal void EnsureInitialized()
        {
            if(!_initialized)
            {
                _monitor.Update(() =>
                {
                    if(!_initialized)
                    {
                        _initialize();
                        _initialized = true;
                    }
                });
            }
        }

        public bool IsInitialized => _initialized;

        internal OptimizedInitializer(Action initialize) => _initialize = initialize;
    }
}
