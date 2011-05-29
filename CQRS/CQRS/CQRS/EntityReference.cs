#region usings

using System;
using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public class EntityReference<TReferencedType, TKey> :
        IdEqualityObject<EntityReference<TReferencedType, TKey>, TKey>,
        IEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>
    {
        protected EntityReference() : base(default(TKey)) {}

        public EntityReference(TKey id) : base(id){}
        public EntityReference(TReferencedType referenced) : base(referenced.Id){}
    }

    public class EntityReference<TReferencedType> :
        EntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid>
    {
        protected EntityReference() {}
        public EntityReference(Guid id) : base(id) {}
        public EntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class NamedEntityReference<TReferencedType, TKey> :
        EntityReference<TReferencedType, TKey>,
        INamedEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>, INamed
    {
        protected NamedEntityReference() {}
        public NamedEntityReference(TKey id, string name) : base(id)
        {
            Name = name;
        }

        public NamedEntityReference(TKey id) : base(id) {}
        public NamedEntityReference(TReferencedType referenced) : base(referenced) {}
        public NamedEntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}

        public string Name { get; private set; }
    }

    public class NamedEntityReference<TReferencedType> :
        NamedEntityReference<TReferencedType, Guid>,
        INamedEntityReference<TReferencedType>
        where TReferencedType : IHasPersistentIdentity<Guid>, INamed
    {
        protected NamedEntityReference() { }
        public NamedEntityReference(Guid id) : base(id) { }
        public NamedEntityReference(Guid id, string name) : base(id, name) {}
        public NamedEntityReference(TReferencedType referenced) : base(referenced) {}
        public NamedEntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}
    }

    public class MaterializableEntityReference<TReferencedType, TKeyType> :
        EntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where
            TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        protected MaterializableEntityReference() { }
        public TReferencedType Referenced { get; private set; }

        public MaterializableEntityReference(TKeyType id) : base(id) {}
        public MaterializableEntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class MaterializableNamedEntityReference<TReferencedType, TKeyType> :
        NamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableNamedEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed
    {
        public TReferencedType Referenced { get; private set; }

        protected MaterializableNamedEntityReference() {}
        public MaterializableNamedEntityReference(TKeyType id) : base(id) {}
        public MaterializableNamedEntityReference(TKeyType id, string name) : base(id, name) { }
        public MaterializableNamedEntityReference(TReferencedType referenced) : base(referenced) {}        
        public MaterializableNamedEntityReference(TReferencedType referenced, string name) : base(referenced, name) {}
    }
}