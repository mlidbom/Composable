using System;

namespace Composable.SystemCE
{
    class OptimizedLazy<TValue> where TValue : class
    {
        readonly object _lock = new object();
        TValue? _value;
        readonly Func<TValue> _factory;

        public TValue Value
        {
            get
            {
                if(_value != null) return _value;

                lock(_lock)
                {
                    return _value ??= _factory();
                }
            }
        }

        public OptimizedLazy(Func<TValue> factory) => _factory = factory;
    }
}
