using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Void.ComponentModel.Properties
{
    [DataContract(Namespace = "")]
    public class Property<TOwnerType, TValueType> : PropertyBase<TValueType>
    {
        //When the object has been created by being deserialized, this is false.....        

        [DataMember(Name = "v")]
        private TValueType _value;

        public Property(Expression<Func<TOwnerType, TValueType>> member) : base(member)
        {
        }

        public TValueType Value
        {
            get
            {
                AssertInitalized();
                return _value;
            }
            set
            {
                AssertInitalized();
                if (!Equals(_value, value))
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}