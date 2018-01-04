using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.Contracts;
using Composable.DDD;
using Composable.System.Reflection;

namespace Composable
{
    // ReSharper disable once RedundantAttributeUsageProperty
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class TypeIdAttribute : Attribute
    {
        internal TypeId Id { get; }

        public TypeIdAttribute(string guid) => Id = new TypeId(Guid.Parse(guid));
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ContainsComposableTypeIdsAttribute : Attribute { }

    class TypeId : ValueObject<TypeId>
    {
        internal readonly Guid GuidValue;

        public TypeId(Guid guidValue)
        {
            Contract.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
        }

        static readonly Dictionary<TypeId, Type> TypeIdToTypemap = CreateTypeMap();

        static Dictionary<TypeId, Type> CreateTypeMap()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                            .Where(me => me.ContainsComposableMessageTypes())
                            .SelectMany(me => me.GetTypes())
                            .Select(me => new {Type = me, TypeId = (me.GetCustomAttribute<TypeIdAttribute>())?.Id})
                            .Where(me => me.TypeId != null)
                            .ToDictionary(me => me.TypeId, me => me.Type);
        }

        public bool TryGetType(out Type type)  => TypeIdToTypemap.TryGetValue(this, out type);

        public Type GetRuntimeType()
        {
            if(!TypeIdToTypemap.TryGetValue(this, out var type))
            {
                throw new Exception($"Failed to map {nameof(TypeId)}:{GuidValue} to a type. Make sure the assembly defining the type is decorated with: {nameof(ContainsComposableTypeIdsAttribute)}");
            }

            return type;
        }

        internal static TypeId FromType(Type eventType)
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
