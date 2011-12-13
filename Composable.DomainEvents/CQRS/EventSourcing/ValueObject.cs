#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;

#endregion

namespace Composable.DDD
{
    ///<summary>Base class for value objects that implements value equality 
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class ValueEqualityEvent<T> : IEquatable<T> where T : ValueEqualityEvent<T>
    {
        /// <see cref="object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            var other = obj as T;

            return Equals(other);
        }

        /// <see cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            var fields = MemberAccessorHelper<T>.GetFieldsAndProperties(GetType());

            const int startValue = 17;
            const int multiplier = 59;

            var hashCode = startValue;

            for (var i = 0; i < fields.Length; i++)
            {
                Contract.Assume(fields[i] != null);
                var value = fields[i](this);

                if (value != null)
                    hashCode = hashCode * multiplier + value.GetHashCode();
            }

            return hashCode;
        }

        /// <see cref="object.Equals(object)"/>
        public virtual bool Equals(T other)
        {
            if (ReferenceEquals(other, null))
                return false;

            var myType = GetType();
            var otherType = other.GetType();

            if (myType != otherType)
                return false;

            var fields = MemberAccessorHelper<T>.GetFieldsAndProperties(GetType());

            for (var i = 0; i < fields.Length; i++)
            {
                Contract.Assume(fields[i] != null);
                var value1 = fields[i](other);
                var value2 = fields[i]((T)this);

                if (ReferenceEquals(value1, null))
                {
                    if (value2 != null)
                        return false;
                }
                else if (!value1.Equals(value2))
                    return false;
            }

            return true;
        }


        ///<summary>Compares the objects for equality using value semantics</summary>
        public static bool operator ==(ValueEqualityEvent<T> lhs, ValueEqualityEvent<T> rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !ReferenceEquals(lhs, null) && lhs.Equals(rhs);
        }

        ///<summary>Compares the objects for inequality using value semantics</summary>
        public static bool operator !=(ValueEqualityEvent<T> lhs, ValueEqualityEvent<T> rhs)
        {
            return !(lhs == rhs);
        }

    }

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

        public static IEnumerable<object> GetFieldAndPropertyValues(object o)
        {
            return GetFieldsAndPropertyGetters(o.GetType()).Select(getter => getter(o));
        } 

        public static Func<Object, Object>[] GetFieldsAndPropertyGetters(Type type)
        {
            return InnerGetField(type);
        }

        private static Func<object, object>[] InnerGetField(Type type)
        {
            Func<Object, Object>[] fields;
            if (!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<Object, object>>();
                if (!type.IsPrimitive)
                {
                    newFields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(BuildFieldGetter));

                    var baseType = type.BaseType;
                    if (baseType != typeof (object))
                    {
                        newFields.AddRange(GetFieldsAndPropertyGetters(baseType));
                    }
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
            Fields = MemberAccessorHelper.GetFieldsAndPropertyGetters(typeof(T));
        }

        public static Func<object, object>[] GetFieldsAndProperties(Type type)
        {
            if(type == typeof(T))
            {
                return Fields;
            }
            return MemberAccessorHelper.GetFieldsAndPropertyGetters(type);
        }
    }
}