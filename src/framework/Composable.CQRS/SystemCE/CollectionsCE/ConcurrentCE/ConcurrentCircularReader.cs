using System.Collections.Generic;
using System.Linq;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

namespace Composable.SystemCE.CollectionsCE.ConcurrentCE
{
    public static class ConcurrentCircularReader
    {
        public static ConcurrentCircularReader<T> ToConcurrentCircularReader<T>(this IEnumerable<T> source) => new ConcurrentCircularReader<T>(source);
    }

    public class ConcurrentCircularReader<T>
    {
        readonly MonitorCE _lock = MonitorCE.WithDefaultTimeout();
        int _current = -1;
        readonly T[] _items;
        public ConcurrentCircularReader(IEnumerable<T> source) => _items = source.ToArray();
        public T Next() => _lock.Update(() =>
        {
            _current++;
            if(_current == _items.Length) _current = 0;
            return _items[_current];
        });
    }
}
