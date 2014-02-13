using System;
using Composable.DDD;

namespace Composable.CQRS
{
    public interface IReferenceObject : IHasPersistentIdentity<Guid>
    {
        
    }
}