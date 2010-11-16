using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Composable.System.ComponentModel.Properties
{
    [DataContract(Namespace = "")]
    public abstract class PropertyBase<TValueType> : IProperty
    {
        private bool _eventInProgress;
        private IPropertyOwner Owner { get; set; }
        private string Name { get; set; }

        public void Initialize(IPropertyOwner owner)
        {
            Owner = owner;
        }

        public virtual event Action<string> PropertyChanged;

        //When deserialized by the DataContractSerializer used by WPF this field will be false.
        private bool _isConstructed = true;

        protected PropertyBase(LambdaExpression member)
        {
            Name = ExpressionUtil.ExtractMemberName(member);
        }

        protected void AssertInitalized()
        {
            if (Owner == null && _isConstructed)
            {
                throw new InvalidOperationException(typeof (TValueType).Name + " Property " + Name + " is not initialized");
            }
        }

        protected void OnPropertyChanged()
        {
            if (!_eventInProgress)
            {
                _eventInProgress = true;
                try
                {
                    if (Owner != null)
                    {
                        Owner.FirePropertyChanged(Name);
                    }
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(Name);
                    }
                }
                finally
                {
                    _eventInProgress = false;
                }
            }
        }

        public void DependsUpon(params IProperty[] others)
        {
            Array.ForEach(others, other => other.PropertyChanged += _ => OnPropertyChanged());
        }
    }
}