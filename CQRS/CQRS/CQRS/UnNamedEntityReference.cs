#region usings

using System;
using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public class UnNamedEntityReference<TReferencedType, TKey> :
        IdEqualityObject<UnNamedEntityReference<TReferencedType, TKey>, TKey>,
        IUnNamedEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>
    {
        protected UnNamedEntityReference() : base(default(TKey)) {}

        public UnNamedEntityReference(TKey id) : base(id){}
        public UnNamedEntityReference(TReferencedType referenced) : base(referenced.Id){}
    }

    public class UnNamedEntityReference<TReferencedType> :
        UnNamedEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid>
    {
        protected UnNamedEntityReference() {}
        public UnNamedEntityReference(Guid id) : base(id) {}
        public UnNamedEntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class EntityReference<TReferencedType, TKey> :
        UnNamedEntityReference<TReferencedType, TKey>,
        IEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>, INamed
    {
        protected EntityReference() {}
        public EntityReference(TKey id, string name) : base(id)
        {
            Name = name;
        }

        public EntityReference(TKey id) : base(id) {}
        public EntityReference(TReferencedType referenced) : base(referenced) {}
        public EntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}

        public virtual string Name { get; set; }
    }

    public class EntityReference<TReferencedType> :        
        EntityReference<TReferencedType, Guid>,
        ITranslatableEntityReference,
        IEntityReference<TReferencedType>,
        IComparable<EntityReference<TReferencedType>> where TReferencedType : IHasPersistentIdentity<Guid>, INamed
    {
        protected EntityReference() { }
        public EntityReference(Guid id) : base(id) { }
        public EntityReference(Guid id, string name) : base(id, name) {}
        public EntityReference(TReferencedType referenced) : base(referenced) {}
        public EntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}
        public int CompareTo(EntityReference<TReferencedType> other)
        {
            return String.Compare(Name, other.Name);
        }
    }

    public interface ITranslatableEntityReference
    {
        Guid Id { get; }
        String Name { get; set; }
    }

    public class MaterializableUnNamedEntityReference<TReferencedType, TKeyType> :
        UnNamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableUnNamedEntityReference<TReferencedType, TKeyType>
        where
            TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        protected MaterializableUnNamedEntityReference() { }
        public TReferencedType Referenced { get; private set; }

        public MaterializableUnNamedEntityReference(TKeyType id) : base(id) {}
        public MaterializableUnNamedEntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class MaterializableEntityReference<TReferencedType, TKeyType> :
        EntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed
    {
        public TReferencedType Referenced { get; private set; }

        protected MaterializableEntityReference() {}
        public MaterializableEntityReference(TKeyType id) : base(id) {}
        public MaterializableEntityReference(TKeyType id, string name) : base(id, name) { }
        public MaterializableEntityReference(TReferencedType referenced) : base(referenced) {}        
        public MaterializableEntityReference(TReferencedType referenced, string name) : base(referenced, name) {}
    }
}