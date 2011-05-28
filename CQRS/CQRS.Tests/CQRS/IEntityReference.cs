using Composable.DDD;

namespace CQRS.Tests.CQRS
{
    public interface IEntityReference<TReferencedType, out TKeyType> where TReferencedType : IPersistentEntity<TKeyType>
    {
        TKeyType Id { get; }
    }

    public interface IMaterializableEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType> where TReferencedType : IPersistentEntity<TKeyType>
    {
        TReferencedType Referenced { get; }
    }

    public interface INamedEntityReference<TReferencedType, out TKeyType> : IEntityReference<TReferencedType, TKeyType> where TReferencedType : IPersistentEntity<TKeyType>
    {
        string Name { get; }
    }

    public interface IMaterializableNamedEntityReference<TReferencedType, out TKeyType> : 
        INamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType> 
        where TReferencedType : IPersistentEntity<TKeyType>
    {
    }  
}