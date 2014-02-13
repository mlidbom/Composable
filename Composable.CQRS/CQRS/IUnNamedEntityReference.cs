#region usings

using System;
using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public interface IUnNamedEntityReference<TReferencedType, out TKeyType> where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TKeyType Id { get; }
    }

    public interface IUnNamedEntityReference<TReferencedType> : IUnNamedEntityReference<TReferencedType, Guid> where TReferencedType : IHasPersistentIdentity<Guid> {}

    public interface IMaterializableUnNamedEntityReference<TReferencedType, out TKeyType> : IUnNamedEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TReferencedType Referenced { get; }
    }

    public interface IMaterializableUnNamedEntityReference<TReferencedType> : IMaterializableUnNamedEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid> {}

    public interface IEntityReference<TReferencedType, out TKeyType> : IUnNamedEntityReference<TReferencedType, TKeyType>, INamed
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed {}

    public interface IEntityReference<TReferencedType> : IEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid>, INamed {}

    public interface IMaterializableEntityReference<TReferencedType, out TKeyType> :
        IEntityReference<TReferencedType, TKeyType>,
        IMaterializableUnNamedEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed {}

    public interface IMaterializableEntityReference<TReferencedType> :
        IEntityReference<TReferencedType>,
        IMaterializableUnNamedEntityReference<TReferencedType>
        where TReferencedType : IHasPersistentIdentity<Guid>, INamed {}
}