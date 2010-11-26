#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;

#endregion

namespace Composable.DDD
{
    ///<summary>Base class for value objects that implements value equality 
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class ValueObject<T> : IEquatable<T> where T : ValueObject<T>
    {
        private static Func<object, object> BuildFieldGetter(FieldInfo field)
        {
            var obj = Expression.Parameter(typeof (object), "obj");

            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Field(
                        Expression.Convert(obj, field.DeclaringType),
                        field),
                    typeof (object)),
                obj).Compile();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            var other = obj as T;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            var fields = GetFields(GetType());

            const int startValue = 17;
            const int multiplier = 59;

            var hashCode = startValue;

            for (int i = 0; i < fields.Length; i++)
            {
                var value = fields[i](this);

                if (value != null)
                    hashCode = hashCode * multiplier + value.GetHashCode();   
            }

            return hashCode;
        }

        public virtual bool Equals(T other)
        {
            if (ReferenceEquals(other, null))
                return false;

            var myType = GetType();
            var otherType = other.GetType();

            if (myType != otherType)
                return false;

            var fields = GetFields(GetType());

            for (int i = 0; i < fields.Length; i++)
            {
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


        private static readonly Func<Object, Object>[] Fields ;

        private static readonly IDictionary<Type, Func<Object, Object>[]> TypeFields =
            new Dictionary<Type, Func<Object, Object>[]>();

        static ValueObject()
        {
            Fields = InnerGetField(typeof(T));
        }

        private static Func<Object, Object>[] GetFields(Type type)
        {
            if (type == typeof(T))
            {
                return Fields;
            }

            lock (TypeFields)
            {
                return InnerGetField(type);
            }
        }

        private static Func<object, object>[] InnerGetField(Type type)
        {
            Func<Object, Object>[] fields;
            if (!TypeFields.TryGetValue(type, out fields))
            {
                var newFields = new List<Func<Object, object>>();
                newFields.AddRange(
                    type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Select(
                        BuildFieldGetter));

                var baseType = type.BaseType;
                if (baseType != typeof (object))
                {
                    newFields.AddRange(GetFields(baseType));
                }

                TypeFields[type] = fields = newFields.ToArray();
            }
            return fields;
        }

        ///<summary>Compares the objects for equality using value semantics</summary>
        public static bool operator ==(ValueObject<T> x, ValueObject<T> y)
        {
            return x.Equals(y);
        }

        ///<summary>Compares the objects for inequality using value semantics</summary>
        public static bool operator !=(ValueObject<T> x, ValueObject<T> y)
        {
            return !(x == y);
        }


        public override string ToString()
        {
            return GetType().FullName + ":" + JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}