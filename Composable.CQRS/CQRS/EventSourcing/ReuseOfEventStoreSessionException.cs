using System;
using Composable.UnitsOfWork;

namespace Composable.CQRS.CQRS.EventSourcing
{
    class ReuseOfEventStoreSessionException : InvalidOperationException
    {
        public ReuseOfEventStoreSessionException(IUnitOfWork current, IUnitOfWork joining):base(string.Format("current: {0}, joining: {1}", current, joining))
        {
        }
    }
}