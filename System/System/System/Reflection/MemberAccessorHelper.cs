using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Composable.System.Reflection
{
    public static class MemberAccessorHelper
    {
        private static readonly IDictionary<Type, Func<Object, Object>[]> TypeFields = new ConcurrentDictionary<Type, Func<Object, Object>[]>();

        private static Func<object, object> BuildFieldGetter(FieldInfo field)
        {
            var obj = Expression.Parameter(typeof(object), "obj");

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Field(
                        Expression.Convert(obj, field.DeclaringType),
                        field),
                    typeof(object)),
                obj).Compile();
        }

        public static Func<Object, Object>[] GetFieldsAndProperties(Type type)
        {
            return InnerGetField(type);
        }

        private static Func<object, object>[] InnerGetField(Type type)
        {
            Func<Object, Object>[] fields;
            if (!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<Object, object>>();
                newFields.AddRange(
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(BuildFieldGetter));

                var baseType = type.BaseType;
                if (baseType != typeof(object))
                {
                    newFields.AddRange(GetFieldsAndProperties(baseType));
                }

                TypeFields[type] = fields = newFields.ToArray();
            }
            Contract.Assume(fields != null);
            return fields;
        }
    }

    public static class MemberAccessorHelper<T>
    {
        public static readonly Func<Object, Object>[] Fields;

        static MemberAccessorHelper()
        {
            Fields = MemberAccessorHelper.GetFieldsAndProperties(typeof(T));
        }

        public static Func<object, object>[] GetFieldsAndProperties(Type getType)
        {
            if(getType == typeof(T))
            {
                return Fields;
            }
            return MemberAccessorHelper.GetFieldsAndProperties(typeof (T));
        }
    }
}