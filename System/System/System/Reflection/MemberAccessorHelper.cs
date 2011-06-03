using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Composable.System.Reflection
{
    public class MemberAccessorHelper<T>
    {
        static MemberAccessorHelper()
        {
            Fields = MemberAccessorHelper<T>.InnerGetField(typeof(T));
        } 

        public static Func<object, object> BuildFieldGetter(FieldInfo field)
        {
            Contract.Requires(field != null);
            var obj = Expression.Parameter(typeof(object), "obj");

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Field(
                        Expression.Convert(obj, field.DeclaringType),
                        field),
                    typeof(object)),
                obj).Compile();
        }

        public static readonly Func<Object, Object>[] Fields;

        public static readonly IDictionary<Type, Func<Object, Object>[]> TypeFields =
            new Dictionary<Type, Func<Object, Object>[]>();

        public static Func<Object, Object>[] GetFields(Type type)
        {
            Contract.Ensures(Contract.Result<Func<object, object>[]>() != null);
            if(type == typeof(T))
            {
                return Fields;
            }

            lock(TypeFields)
            {
                return InnerGetField(type);
            }
        }

        public static Func<object, object>[] InnerGetField(Type type)
        {
            Contract.Requires(type != null);
            Contract.Ensures(Contract.Result<Func<object, object>[]>() != null);
            Func<Object, Object>[] fields;
            if(!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<Object, object>>();
                newFields.AddRange(
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(
                        BuildFieldGetter));

                var baseType = type.BaseType;
                if(baseType != typeof(object))
                {
                    newFields.AddRange(GetFields(baseType));
                }

                TypeFields[type] = fields = newFields.ToArray();
            }
            Contract.Assume(fields != null);
            return fields;
        }
    }
}