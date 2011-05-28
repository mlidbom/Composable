#region usings

using Composable.DDD;

#endregion

namespace Composable.CQRS
{
    public interface IEntityReference<TReferencedType, out TKeyType> where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TKeyType Id { get; }
    }

    public interface IMaterializableEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        TReferencedType Referenced { get; }
    }

    public interface INamedEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType>
        where TReferencedType :
            IHasPersistentIdentity<TKeyType>, INamed
    {
        string Name { get; }
    }

    public interface IMaterializableNamedEntityReference<TReferencedType, out TKeyType> :
        INamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed {}
}