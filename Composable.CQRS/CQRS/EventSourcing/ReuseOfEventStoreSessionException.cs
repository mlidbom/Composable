using System;
using Composable.UnitsOfWork;

namespace Composable.CQRS.CQRS.EventSourcing
{
    class ReuseOfEventStoreSessionException : InvalidOperationException
    {
        public ReuseOfEventStoreSessionException(IUnitOfWork current, IUnitOfWork joining):base($"current: {current}, joining: {joining}")
        {
        }
    }
}