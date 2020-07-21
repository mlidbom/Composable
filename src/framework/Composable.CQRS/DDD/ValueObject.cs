using System;
using System.Collections;
using System.Linq;
using Composable.Serialization;
using Composable.System.Reflection;
using Newtonsoft.Json;

namespace Composable.DDD
{
    //Review:mlidbo: Consider whether comparing using public properties only would make more sense. Maybe separate class?
    ///<summary>
    /// Base class for value objects that implements value equality based on instance fields.
    /// Properties are ignored when comparing. Only fields are used.
    ///</summary>
    ///<typeparam name="T"></typeparam>
    public abstract class ValueObject<T> : IEquatable<T> where T : ValueObject<T>
    {
        /// <see cref="object.Equals(object)"/>
        public override bool Equals(object? obj) => obj is T other && Equals(other);

        /// <see cref="object.GetHashCode"/>
        public override int GetHashCode()
        {
            var fields = MemberAccessorHelper<T>.GetFieldGetters(GetType());

            const int startValue = 17;
            const int multiplier = 59;

            var hashCode = startValue;

            // ReSharper disable once ForCanBeConvertedToForeach optimization
            for(var i = 0; i < fields.Length; i++)
            {
                var value = fields[i](this);

                if (value is IEnumerable enumerableValue && !(value is string))
                {
                    var value1Array = enumerableValue.Cast<object?>().Where(me => !(me is null)).ToArray();
                    foreach(var something in value1Array)
                    {
                        hashCode = hashCode * multiplier + something!.GetHashCode();
                    }
                }
                else if(!(value is null))
                    hashCode = hashCode * multiplier + value.GetHashCode();
            }

            return hashCode;
        }

        /// <see cref="object.Equals(object)"/>
        public virtual bool Equals(T? other)
        {
            if(other is null)
                return false;

            var myType = GetType();
            var otherType = other.GetType();

            if(myType != otherType)
                return false;

            var fields = MemberAccessorHelper<T>.GetFieldGetters(GetType());

            foreach(var fieldGetter in fields)
            {
                var value1 = fieldGetter(other);
                var value2 = fieldGetter((T)this);

                if(value1 is null)
                {
                    if(!(value2 is null))
                        return false;
                }
                else if (value1 is IEnumerable valueAsEnumerable && !(value1 is string))
                {
                    if (value2 is null)
                    {
                        return false;
                    }
                    var value1Array = valueAsEnumerable.Cast<object>().ToArray();
                    var value2Array = ((IEnumerable)value2).Cast<object>().ToArray();
                    if (value1Array.Length != value2Array.Length)
                    {
                        return false;
                    }
                    for (var j = 0; j < value1Array.Length ; ++j)
                    {
                        if (!Equals(value1Array[j], value2Array[j]))
                        {
                            return false;
                        }
                    }
                }
                else if(!value1.Equals(value2))
                    return false;
            }

            return true;
        }


        ///<summary>Compares the objects for equality using value semantics</summary>
        public static bool operator ==(ValueObject<T>? lhs, ValueObject<T>? rhs)
        {
            if(ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            return !(lhs is null) && lhs.Equals(rhs);
        }

        ///<summary>Compares the objects for inequality using value semantics</summary>
        public static bool operator !=(ValueObject<T> lhs, ValueObject<T> rhs) => !(lhs == rhs);

        ///<returns>A JSON serialized version of the instance.</returns>
        public override string ToString()
        {
            try
            {
                return GetType().FullName + ":" + JsonConvert.SerializeObject(this, JsonSettings.JsonSerializerSettings);
            }
            catch (Exception)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return GetType().FullName;
            }
        }
    }
}