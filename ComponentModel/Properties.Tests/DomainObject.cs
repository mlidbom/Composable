#region using

using System.ComponentModel;
using System.Runtime.Serialization;

#endregion

namespace Void.ComponentModel.Properties.Tests
{
    [DataContract]
    public class DomainObject : IPropertyOwner
    {
        [DataMember]
        private readonly Property<DomainObject, string> _dependentProperty = new Property<DomainObject, string>(me => me.DependentProperty);
        public string DependentProperty { get { return _dependentProperty.Value; } set { _dependentProperty.Value = value; } }

        [DataMember]
        private readonly Property<DomainObject, string> _standAloneProperty = new Property<DomainObject, string>(me => me.StandAloneProperty);
        public string StandAloneProperty { get { return _standAloneProperty.Value; } set { _standAloneProperty.Value = value; } }

        public DomainObject()
        {
            _standAloneProperty.Initialize(this);
            _dependentProperty.Initialize(this);
            _dependentProperty.DependsUpon(_standAloneProperty);
        }

        #region IPropertyOwner Members

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public void FirePropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}