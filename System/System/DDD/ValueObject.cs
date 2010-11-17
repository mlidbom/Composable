using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Linq;

namespace Composable.DDD
{
    ///<summary>Base class for value objects that implements value equality 
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class ValueObject<T> : IEquatable<T> where T : ValueObject<T>
    {
        private static Func<object, object> BuildFieldGetter(FieldInfo fieldInfo)
        {
            var param = Expression.Parameter(typeof(object), "obj");

            var castParam = Expression.Convert(param, fieldInfo.DeclaringType);

            var lambda = Expression.Lambda<Func<object, object>>(Expression.Field(castParam, fieldInfo), param);

            return lambda.Compile();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var other = obj as T;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            var fields = GetFields(this.GetType());

            const int startValue = 17;
            const int multiplier = 59;

            int hashCode = startValue;

            foreach (var field in fields)
            {
                var value = field(this);

                if (value != null)
                    hashCode = hashCode * multiplier + value.GetHashCode();
            }

            return hashCode;
        }

        public virtual bool Equals(T other)
        {
            if (other == null)
                return false;

            var myType = GetType();
            var otherType = other.GetType();

            if (myType != otherType)
                return false;

            var fields = GetFields(GetType());

            foreach (var fieldGetter in fields)
            {
                object value1 = fieldGetter(other);
                object value2 = fieldGetter((T) this);

                if (value1 == null)
                {
                    if (value2 != null)
                        return false;
                }
                else if (!value1.Equals(value2))
                    return false;
            }

            return true;
        }

        public static readonly IDictionary<Type, IEnumerable<Func<Object,Object>>> TypeFields = new Dictionary<Type, IEnumerable<Func<object, object>>>();
        private IEnumerable<Func<Object,Object>> GetFields(Type type)
        {
            lock (TypeFields)
            {
                IEnumerable<Func<Object, Object>> fields;
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

                    TypeFields[type] = newFields;
                    fields = newFields;
                }
                return fields;
            }            
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
    }
}