#region usings

using System;
using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public class EntityReference<TReferencedType, TKey> :
        IEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>
    {
        protected EntityReference() {}

        protected EntityReference(TKey id)
        {
            Id = id;
        }

        public TKey Id { get; private set; }
    }

    public class NamedEntityReference<TReferencedType, TKey> :
        EntityReference<TReferencedType, TKey>,
        INamedEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>, INamed
    {
        protected NamedEntityReference(TKey id):base(id)
        {            
        }

        public string Name { get; private set; }
    }

    public class MaterializableEntityReference<TReferencedType, TKeyType> :
        EntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where
            TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        public TReferencedType Referenced { get { throw new NotImplementedException(); } }
    }

    public class MaterializableNamedEntityReference<TReferencedType, TKeyType> :
        MaterializableEntityReference<TReferencedType, TKeyType>,
        IMaterializableNamedEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed
    {
        public string Name { get; private set; }
    }
}