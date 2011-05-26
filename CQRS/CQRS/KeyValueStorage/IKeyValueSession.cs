using System;

namespace Composable.KeyValueStorage
{
    public interface IKeyValueSession : IDisposable
    {
        TValue Load<TValue>(Guid key);
        void Save<TValue>(Guid key, TValue value);
        void SaveChanges();
    }
}