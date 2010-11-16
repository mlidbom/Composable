using System;
using System.ComponentModel;
using NUnit.Framework;

namespace Composable.System.ComponentModel.Properties.Tests.AutoProperty
{
    [TestFixture]
    public class WhenPropertyValueChanges
    {
        protected virtual DomainObject ProvideInstance()
        {
            return new DomainObject();
        }

        private static void EnsureNoChangeEventRaisedForProperty(INotifyPropertyChanged instance, string propertyName, Action mutator)
        {
            var raised = CheckIfRaised(instance, propertyName, mutator);
            Assert.That(raised, Is.False);
        }

        private static void EnsureChangeRaisedForProperty(INotifyPropertyChanged instance, string propertyName, Action mutator)
        {
            var raised = CheckIfRaised(instance, propertyName, mutator);
            Assert.That(raised, Is.True);
        }

        private static bool CheckIfRaised(INotifyPropertyChanged instance, string propertyName, Action mutator)
        {
            var raised = false;
            instance.PropertyChanged += (_, args) =>
                                            {
                                                if (args.PropertyName == propertyName)
                                                {
                                                    raised = true;
                                                }
                                            };
            mutator();
            return raised;
        }

        [Test]
        public void AssignedValueShouldBeReturned()
        {
            var instance = ProvideInstance();
            instance.StandAloneProperty = "yes";

            Assert.That(instance.StandAloneProperty, Is.EqualTo("yes"));

            instance.StandAloneProperty = null;
            Assert.That(instance.StandAloneProperty, Is.Null);
        }

        [Test]
        public void NoEventShouldBeRaisedWhenPropertySetToSameValue()
        {
            var instance = ProvideInstance();
            instance.StandAloneProperty = "1";

            EnsureNoChangeEventRaisedForProperty(instance, "StandAloneProperty", () => instance.StandAloneProperty = "1");

            instance.StandAloneProperty = null;
            EnsureNoChangeEventRaisedForProperty(instance, "StandAloneProperty", () => instance.StandAloneProperty = null);
        }

        [Test]
        public virtual void PropertyChangedShouldBeFiredForTheChangedProperty()
        {
            var instance = ProvideInstance();
            EnsureChangeRaisedForProperty(instance, "StandAloneProperty", () => instance.StandAloneProperty = "Some string");
            EnsureChangeRaisedForProperty(instance, "StandAloneProperty", () => instance.StandAloneProperty = null);
        }

        [Test]
        public virtual void PropertyChangedShouldBeRaisedForDependingPropertyWhenTargetPropertyChanged()
        {
            var instance = ProvideInstance();
            EnsureChangeRaisedForProperty(instance, "DependentProperty", () => instance.StandAloneProperty = "some string");
        }
    }
}