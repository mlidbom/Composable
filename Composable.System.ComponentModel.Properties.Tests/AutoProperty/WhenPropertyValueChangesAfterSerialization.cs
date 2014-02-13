#region usings

using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;

#endregion

namespace Composable.Tests.System.ComponentModel.Properties.AutoProperty
{
    [TestFixture]
    public class WhenPropertyValueChangesAfterSerialization : WhenPropertyValueChanges
    {
        protected override DomainObject ProvideInstance()
        {
            using(var dataStream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(DomainObject));
                serializer.WriteObject(dataStream, new DomainObject());
                dataStream.Position = 0;
                return (DomainObject)serializer.ReadObject(dataStream);
            }
        }

        [Ignore("Not implemented unsure if it's possible to implement in a sane way")]
        public override void PropertyChangedShouldBeFiredForTheChangedProperty()
        {
            base.PropertyChangedShouldBeFiredForTheChangedProperty();
        }

        [Ignore("Not implemented unsure if it's possible to implement in a sane way")]
        public override void PropertyChangedShouldBeRaisedForDependingPropertyWhenTargetPropertyChanged()
        {
            base.PropertyChangedShouldBeRaisedForDependingPropertyWhenTargetPropertyChanged();
        }
    }
}