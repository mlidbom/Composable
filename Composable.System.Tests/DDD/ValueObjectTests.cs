#region usings

using System;
using Composable.DDD;
using NUnit.Framework;

#endregion

namespace Composable.Tests.DDD
{
    [TestFixture]
    public class ValueObjectTests
    {
        private class Address : ValueObject<Address>
        {
            string _address1;
            private readonly string _city;
            private readonly string[] _states;

            public Address(string address1, string city, params string[] states)
            {
                _address1 = address1;
                _city = city;
                _states = states;
            }

            public Address()
            {}

            //todo: fix this crazy behavior. These tests should fail!
            public string Address1 { get { return null; } }//ncrunch: no coverage

            public string City { get { return null; } }//ncrunch: no coverage

            public string Guid { get; set; }

            public string[] States { get { return null; } }//ncrunch: no coverage
        }

        private class GuidHolder : ValueObject<GuidHolder>
        {
            public GuidHolder(Guid id)
            {
                Id = id;
            }

            public Guid Id { get; private set; }
        }

        private class ExpandedAddress : Address
        {
            private readonly string _address2;

            public ExpandedAddress(string address1, string address2, string city, params string[] states)
                : base(address1, city, states)
            {
                _address2 = address2;
            }

            public string Address2 { get { return _address2; } }
        }

        [Test]
        public void AddressEqualsWorksWithIdenticalAddresses()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");

            Assert.IsTrue(address.Equals(address2));
        }

        [Test]
        public void GuidEqualsWorksWithIdenticalGuid()
        {
            var id = Guid.NewGuid();
            var guid1 = new GuidHolder(id);
            var guid2 = new GuidHolder(id);

            Assert.IsTrue(guid1.Equals(guid2));
        }

        [Test]
        public void GuidEqualsWorksWithNonIdenticalGuid()
        {
            var guid1 = new GuidHolder(Guid.NewGuid());
            var guid2 = new GuidHolder(Guid.NewGuid());

            Assert.IsFalse(guid1.Equals(guid2));
        }

        [Test]
        public void AddressEqualsWorksWithNonIdenticalGuids()
        {
            var address = new Address("Address1", "Austin", "TX") { Guid = "test" };
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.IsFalse(address.Equals(address2));
        }

        [Test]
        public void AddressEqualsWorksWithNulls()
        {
            var address = new Address(null, "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.IsFalse(address.Equals(address2));
        }

        [Test]
        public void AddressEqualsWorksWithNullsOnOtherObject()
        {
            var address = new Address("Address2", "Austin", "TX");
            var address2 = new Address("Address2", null, "TX");

            Assert.IsFalse(address.Equals(address2));
        }

        [Test]
        public void AddressEqualsIsReflexive()
        {
            var address = new Address("Address1", "Austin", "TX");

            Assert.IsTrue(address.Equals(address));
        }

        [Test]
        public void AddressEqualsIsSymmetric()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.IsFalse(address.Equals(address2));
            Assert.IsFalse(address2.Equals(address));
        }

        [Test]
        public void AddressEqualsIsTransitive()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");
            var address3 = new Address("Address1", "Austin", "TX");

            Assert.IsTrue(address.Equals(address2));
            Assert.IsTrue(address2.Equals(address3));
            Assert.IsTrue(address.Equals(address3));
        }

        [Test]
        public void AddressOperatorsWork()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");
            var address3 = new Address("Address2", "Austin", "TX");

            Assert.IsTrue(address == address2);
            Assert.IsTrue(address2 != address3);
        }

        [Test]
        public void DerivedTypesBehaveCorrectly()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new ExpandedAddress("Address1", "Apt 123", "Austin", "TX");

            Assert.IsFalse(address.Equals(address2));
            Assert.IsFalse(address == address2);
        }

        [Test]
        public void EqualValueObjectsHaveSameHashCode()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX");

            Assert.AreEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void EqualValuesInEnumerableObjectsHaveSameHashCode()
        {
            var address = new Address("Address1", "Austin", "TX", "BB");
            var address2 = new Address("Address1", "Austin", "TX", "BB");

            Assert.AreEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void DifferentNumberOfEntriesInArrayMeansObjectAreNotEqual()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.AreNotEqual(address, address2);
        }

        [Test]
        public void DifferentNumberOfEntriesInArrayMeansHashAreNotEqual()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.AreNotEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void MultipleValuesInEnumerableAreStillEqual()
        {
            var address = new Address("Address1", "Austin", "TX", "TX");
            var address2 = new Address("Address1", "Austin", "TX", "TX");

            Assert.AreEqual(address, address2);
        }

        [Test]
        public void EnumerablesHandleNulls()
        {
            var address = new Address("Address1", "Austin", "TX", null, "TX");
            var address2 = new Address("Address1", "Austin", "TX", null, "TX");

            Assert.AreEqual(address, address2);
        }

        [Test]
        public void TransposedValuesGiveDifferentHashCodes()
        {
            var address = new Address(null, "Austin", "TX");
            var address2 = new Address("TX", "Austin", null);

            Assert.AreNotEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void UnequalValueObjectsHaveDifferentHashCodes()
        {
            var address = new Address("Address1", "Austin", "TX");
            var address2 = new Address("Address2", "Austin", "TX");

            Assert.AreNotEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void TransposedValuesOfFieldNamesGivesDifferentHashCodes()
        {
            var address = new Address("_city", null, null);
            var address2 = new Address(null, "_address1", null);

            Assert.AreNotEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void DerivedTypesHashCodesBehaveCorrectly()
        {
            var address = new ExpandedAddress("Address99999", "Apt 123", "New Orleans", "LA");
            var address2 = new ExpandedAddress("Address1", "Apt 123", "Austin", "TX");

            Assert.AreNotEqual(address.GetHashCode(), address2.GetHashCode());
        }

        [Test]
        public void ComparisonWithRhsNullReturnsFalse()
        {
            var lhs = new Address();
            Assert.That(lhs.Equals(null), Is.False);
            Assert.That(lhs == null, Is.False);
        }

        [Test]
        public void ComparisonWithLhsNullReturnsFalse()
        {
            var rhs = new Address();
            Assert.That(null == rhs, Is.False);
        }

        [Test]
        public void ComparisonWithLhsNullAndRhsNullReturnsTrue()
        {
            Address rhs = null;
            Address lhs = null;
            Assert.That(rhs == lhs, Is.True);
        }
    }
}