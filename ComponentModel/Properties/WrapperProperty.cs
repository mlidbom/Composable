using System;
using System.Linq.Expressions;

namespace Composable.System.ComponentModel.Properties
{
    public class WrapperProperty<T> : PropertyBase<T>
    {
        public WrapperProperty(Expression<Func<T>> member, Func<T> getter, Action<T> setter) : base(member)
        {
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }

            if (getter == null)
            {
                throw new ArgumentNullException("getter");
            }

            if (setter == null)
            {
                throw new ArgumentNullException("setter");
            }
            SetValue = setter;
        }

        private Action<T> SetValue { get; set; }
        private Func<T> GetValue { get; set; }

        public T Value
        {
            get { return GetValue(); }
            set
            {
                if (!Equals(value, GetValue()))
                {
                    SetValue(value);
                    OnPropertyChanged();
                }
            }
        }
    }
}