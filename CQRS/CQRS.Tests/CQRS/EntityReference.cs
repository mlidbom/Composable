#region usings

using System;
using Composable.DDD;

#endregion

namespace CQRS.Tests.CQRS
{
    public class EntityReference<TReferencedType, TKey> :
        IEntityReference<TReferencedType, TKey>
        where TReferencedType : EntityReference<TReferencedType, TKey>, IPersistentEntity<TKey>
    {
        public TKey Id { get; private set; }
    }

    public class NamedEntityReference<TReferencedType, TKey> :
        EntityReference<TReferencedType, TKey>,
        INamedEntityReference<TReferencedType, TKey>
        where TReferencedType : NamedEntityReference<TReferencedType, TKey>, IPersistentEntity<TKey>
    {
        public string Name { get; private set; }
    }

    public class MaterializableEntityReference<TReferencedType, TKeyType> :
        EntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where
            TReferencedType : MaterializableEntityReference<TReferencedType, TKeyType>,
            IPersistentEntity<TKeyType>
    {
        public TReferencedType Referenced { get { throw new NotImplementedException(); } }
    }

    public class MaterializableNamedEntityReference<TReferencedType, TKeyType> :
        MaterializableEntityReference<TReferencedType, TKeyType>,
        IMaterializableNamedEntityReference<TReferencedType, TKeyType>
        where TReferencedType : MaterializableEntityReference<TReferencedType, TKeyType>, IPersistentEntity<TKeyType>
    {
        public string Name { get; private set; }
    }
}