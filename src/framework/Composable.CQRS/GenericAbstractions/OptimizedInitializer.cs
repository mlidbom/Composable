using System;

namespace Composable.GenericAbstractions
{
    public class OptimizedInitializer
    {
        readonly object _lock = new object();
        bool _initialized;
        readonly Action _initialize;

        internal void EnsureInitialized()
        {
            if(!_initialized)
            {
                lock(_lock)
                {
                    if(!_initialized)
                    {
                        _initialize();
                        _initialized = true;
                    }
                }
            }
        }

        public bool IsInitialized => _initialized;

        internal OptimizedInitializer(Action initialize) => _initialize = initialize;
    }
}
