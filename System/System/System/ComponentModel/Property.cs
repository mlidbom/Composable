using System;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Composable.System.ComponentModel.Properties
{
    [DataContract(Namespace = "")]
    public class Property<TOwnerType, TValueType> : PropertyBase<TValueType>
    {
        //When the object has been created by being deserialized, this is false.....        

        [DataMember(Name = "v")]
        private TValueType _value;

        public Property(Expression<Func<TOwnerType, TValueType>> member) : base(member)
        {
            Contract.Requires(member!=null);
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