using System;
using Composable.UnitsOfWork;

namespace Composable.Persistence.EventStore
{
    class ReuseOfEventStoreSessionException : InvalidOperationException
    {
        public ReuseOfEventStoreSessionException(IUnitOfWork current, IUnitOfWork joining):base($"current: {current}, joining: {joining}")
        {
        }
    }
}