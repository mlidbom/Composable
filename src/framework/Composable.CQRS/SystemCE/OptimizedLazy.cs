using System;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE
{
    class OptimizedLazy<TValue> where TValue : class
    {
        readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();
        TValue? _value;
        readonly Func<TValue> _factory;

        public TValue Value
        {
            get
            {
                if(_value != null) return _value;

                return _monitor.Update(() => _value ??= _factory());
            }
        }

        public OptimizedLazy(Func<TValue> factory) => _factory = factory;
    }
}
