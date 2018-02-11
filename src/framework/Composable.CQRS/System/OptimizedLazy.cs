using System;

namespace Composable.System
{
    class OptimizedLazy<TValue> where TValue : class
    {
        readonly object _lock = new object();
        TValue _value;
        readonly Func<TValue> _factory;

        public TValue Value
        {
            get
            {
                if(_value == null)
                {
                    lock(_lock)
                    {
                        if(_value == null)
                        {
                            _value = _factory();
                        }
                    }
                }

                return _value;
            }
        }

        public OptimizedLazy(Func<TValue> factory) => _factory = factory;
    }
}
