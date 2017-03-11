using Composable.CQRS.KeyValueStorage;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public class DocumentDBSession_DocumentKeyTests
    {
        class Base
        {}

        class Inheritor : Base
        {}

        class Unrelated{}


        [Test]
        public void TwoInstancesOfTheSameTypeWithTheSameIdAreEqualAndHaveTheSameHashCode()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theId");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
            lhs.GetHashCode().Should().Be(rhs.GetHashCode());
        }

        [Test]
        public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyByCaseAreEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("THEID");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
            lhs.GetHashCode().Should().Be(rhs.GetHashCode());
        }

        [Test]
        public void TwoInstancesOfTheSameTypeWithIdsDifferingOnlyInTrailingSpacesAreEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theid  ");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theid");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
            lhs.GetHashCode().Should().Be(rhs.GetHashCode());
        }

        [Test]
        public void TwoInstancesOfTheSameTypeWithDifferentIdsAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theSecondId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }

        [Test]
        public void TwoInstancesWithInheritingTypesAndTheSameIdAreEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theId");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
            lhs.GetHashCode().Should().Be(rhs.GetHashCode());
        }

        [Test]
        public void TwoInstancesWithInheritingTypesAndDifferingIdsAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
            var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theSecondId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }

        [Test]
        public void TwoInstancesOfUnrelatedTypesAndSameIdAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Unrelated>("theId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }
    }
}