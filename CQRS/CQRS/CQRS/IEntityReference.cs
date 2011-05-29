#region usings

using System;
using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public interface IEntityReference<TReferencedType, out TKeyType> where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TKeyType Id { get; }
    }

    public interface IEntityReference<TReferencedType> : IEntityReference<TReferencedType, Guid> where TReferencedType : IHasPersistentIdentity<Guid> {}

    public interface IMaterializableEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TReferencedType Referenced { get; }
    }

    public interface IMaterializableEntityReference<TReferencedType> : IMaterializableEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid> {}

    public interface INamedEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType>, INamed
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed {}

    public interface INamedEntityReference<TReferencedType> : INamedEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid>, INamed {}

    public interface IMaterializableNamedEntityReference<TReferencedType, out TKeyType> :
        INamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed {}

    public interface IMaterializableNamedEntityReference<TReferencedType> :
        INamedEntityReference<TReferencedType>,
        IMaterializableEntityReference<TReferencedType>
        where TReferencedType : IHasPersistentIdentity<Guid>, INamed {}
}