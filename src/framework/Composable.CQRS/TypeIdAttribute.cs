using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Composable.Contracts;
using Composable.DDD;
using Composable.System.Linq;
using Composable.System.Reflection;

namespace Composable
{
    // ReSharper disable once RedundantAttributeUsageProperty
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
    public class TypeIdAttribute : Attribute
    {
        internal Guid Id { get; }

        public TypeIdAttribute(string guid) => Id = Guid.Parse(guid);
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    public class ContainsComposableTypeIdsAttribute : Attribute { }

    class TypeId : ValueObject<TypeId>
    {
        internal readonly Guid GuidValue;
        internal readonly Guid ParentTypeGuidValue;

        public override string ToString() => $"{GuidValue}:{ParentTypeGuidValue}";

        internal TypeId(Guid guidValue, Guid parentTypeGuidValue)
        {
            Contract.Argument.Assert(guidValue != Guid.Empty);
            GuidValue = guidValue;
            ParentTypeGuidValue = parentTypeGuidValue;
        }

        internal bool TryGetRuntimeType(out Type type)
        {
            lock(Lock)
            {
                type = null;
                if(TypeIdToTypeMap.TryGetValue(this, out type)) return true;

                if(ParentTypeGuidValue == Guid.Empty) return false;

                if(!TypeIdToTypeMap.TryGetValue(new TypeId(ParentTypeGuidValue, Guid.Empty), out var genericTypeArgument)) return false;

                if(!TypeIdToTypeMap.TryGetValue(new TypeId(GuidValue, Guid.Empty), out var genericType)) return false;

                type = genericType.MakeGenericType(genericTypeArgument);
                TypeIdToTypeMap.Add(this, type);
                TypeToTypeIdMap.Add(type, this);
                return true;

            }
        }

        internal Type GetRuntimeType()
        {
            lock(Lock)
            {
                if(!TypeIdToTypeMap.TryGetValue(this, out var type))
                {
                    throw new Exception($"Failed to map {nameof(TypeId)}:{GuidValue} to a type. Make sure the assembly defining the type is decorated with: {nameof(ContainsComposableTypeIdsAttribute)}");
                }
                return type;
            }
        }

        internal static TypeId FromType(Type type)
        {
            lock(Lock)
            {
                if(TypeToTypeIdMap.TryGetValue(type, out var typeId))
                {
                    return typeId;
                }

                var attribute = type.GetCustomAttribute<TypeIdAttribute>();
                if(attribute == null)
                {
                    //todo: check if we can get newtonsoft to use this to identify types instead of the name.
                    throw new Exception($"Type: {type.FullName} must have a {typeof(TypeIdAttribute).FullName}. It is used to refer to the type in persisted data. By requiring this attribute we can give you the ability to rename or move types without breaking anything.");
                }

                if(type.IsConstructedGenericType)
                {
                    var typeParameters = type.GenericTypeArguments;
                    if(typeParameters.Length != 1)
                    {
                        throw new Exception($"A generic type with a TypeId attribute must have exactly one Type parameter. This is not followed by: {type}");
                    }

                    var typeParameter = typeParameters.Single();

                    var typeParameterTypeId = FromType(typeParameter);

                    typeId = new TypeId(attribute.Id, typeParameterTypeId.GuidValue);
                } else
                {
                    typeId = new TypeId(attribute.Id, Guid.Empty);
                }

                TypeToTypeIdMap.Add(type, typeId);
                TypeIdToTypeMap.Add(typeId, type);

                return typeId;
            }
        }

        static readonly object Lock = new object();
        static readonly Dictionary<TypeId, Type> TypeIdToTypeMap = new Dictionary<TypeId, Type>();
        static readonly Dictionary<Type, TypeId> TypeToTypeIdMap = new Dictionary<Type, TypeId>();

        static TypeId()
        {
            AppDomain.CurrentDomain.GetAssemblies()
                                     .Where(me => me.ContainsComposableMessageTypes())
                                     .SelectMany(me => me.GetTypes())
                                     .Where(me => me.GetCustomAttribute<TypeIdAttribute>() != null)
                                     .ForEach(TypeId.FromType);
        }

        public static TypeId Parse(string eventTypeId)
        {
            var primaryGuid = eventTypeId.Substring(0, 36);
            var secondaryGuid = eventTypeId.Substring(37, 36);

            return new TypeId(Guid.Parse(primaryGuid), Guid.Parse(secondaryGuid));
        }
    }
}
