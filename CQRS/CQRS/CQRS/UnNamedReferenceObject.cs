using System;
using Composable.DDD;

namespace Composable.CQRS
{
    public class UnNamedReferenceObject<TObject> : PersistentEntity<TObject> ,
        IReferenceObject
        where TObject : PersistentEntity<TObject>
    {
        protected UnNamedReferenceObject(Guid id):base(id)
        {            
        }

        protected UnNamedReferenceObject()
        {
        }
    }
}