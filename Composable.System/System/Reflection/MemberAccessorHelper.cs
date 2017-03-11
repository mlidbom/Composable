using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Composable.Contracts;

namespace Composable.System.Reflection
{
    ///<summary>Provides high performance access to object fields and properties.</summary>
    static class MemberAccessorHelper
    {
        static readonly IDictionary<Type, Func<Object, Object>[]> TypeFields = new ConcurrentDictionary<Type, Func<Object, Object>[]>();

        static Func<object, object> BuildFieldGetter(FieldInfo field)
        {
            ContractOptimized.Argument(field, nameof(field), field.DeclaringType, "field.DeclaringType")
                             .NotNull();

            var obj = Expression.Parameter(typeof(object), "obj");

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Field(
                                     // ReSharper disable once AssignNullToNotNullAttribute
                        Expression.Convert(obj, field.DeclaringType),
                        field),
                    typeof(object)),
                obj).Compile();
        }

        ///<summary>Returns functions that when invoked will return the values of the fields an properties in an instance of the supplied type.</summary>
        public static Func<Object, Object>[] GetFieldGetters(Type type)
        {
            ContractOptimized.Argument(type, nameof(type))
                             .NotNull();

            return InnerGetFields(type);
        }

        static Func<object, object>[] InnerGetFields(Type type)
        {
            Func<Object, Object>[] fields;

            if (!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<Object, object>>();
                if (!type.IsPrimitive)
                {
                    newFields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(BuildFieldGetter));

                    var baseType = type.BaseType;
                    if (baseType != null && baseType != typeof (object))
                    {
                        newFields.AddRange(GetFieldGetters(baseType));
                    }
                }
                TypeFields[type] = fields = newFields.ToArray();
            }
            return fields;
        }
    }

    ///<summary>Provides high performance access to object fields and properties.</summary>
    static class MemberAccessorHelper<T>
    {
        // ReSharper disable StaticFieldInGenericType
        static readonly Func<Object, Object>[] Fields;
        // ReSharper restore StaticFieldInGenericType

        static MemberAccessorHelper()
        {
            Fields = MemberAccessorHelper.GetFieldGetters(typeof(T));
        }

        ///<summary>Returns functions that when invoked will return the values of the fields an properties in an instance of the supplied type.</summary>
        public static Func<object, object>[] GetFieldGetters(Type type)
        {
            ContractOptimized.Argument(type, nameof(type))
                             .NotNull();

            if(type == typeof(T))
            {
                return Fields;
            }
            return MemberAccessorHelper.GetFieldGetters(type);
        }
    }
}