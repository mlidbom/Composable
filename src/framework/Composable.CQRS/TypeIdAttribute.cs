using System;
using System.Reflection;

namespace Composable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class TypeIdAttribute : Attribute
    {
        public Guid Id { get; }

        public TypeIdAttribute(string guid) => Id = Guid.Parse(guid);

        public static Guid Extract(Type eventType)
        {
            var attribute = eventType.GetCustomAttribute<TypeIdAttribute>();
            if(attribute == null)
            {
                //todo: check if we can get newtonsoft to use this to identify types instead of the name.
                throw new Exception($"Type: {eventType.FullName} must have a {typeof(TypeIdAttribute).FullName}. It is used to refer to the type in persisted data. By requiring this attribute we can give you the ability to rename or move types without breaking anything.");
            }
            return attribute.Id;
        }
    }
}
