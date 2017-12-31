using System;
using System.Reflection;
using Composable.Contracts;
using Composable.DDD;

namespace Composable
{
    // ReSharper disable once RedundantAttributeUsageProperty
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class TypeIdAttribute : Attribute
    {
        public Guid Id { get; }

        public TypeIdAttribute(string guid) => Id = Guid.Parse(guid);

        internal static TypeId Extract(object instance) => Extract(instance.GetType());

        internal static TypeId Extract(Type eventType)
        {
            var attribute = eventType.GetCustomAttribute<TypeIdAttribute>();
            if(attribute == null)
            {
                //todo: check if we can get newtonsoft to use this to identify types instead of the name.
                throw new Exception($"Type: {eventType.FullName} must have a {typeof(TypeIdAttribute).FullName}. It is used to refer to the type in persisted data. By requiring this attribute we can give you the ability to rename or move types without breaking anything.");
            }
            return new TypeId(attribute.Id);
        }
    }

    class TypeId : ValueObject<TypeId>
    {
        internal readonly Guid GuidValue;
        public TypeId(Guid guidValue)
        {
            Contract.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }
    }
}
