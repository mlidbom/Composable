using System;

namespace Composable.KeyValueStorage
{
    public class NoSuchKeyException : Exception
    {
        public NoSuchKeyException(Guid key):base(key.ToString())
        {
        }
    }
}