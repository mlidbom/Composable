#region usings

using NUnit.Framework;

#endregion

namespace Composable
{
    [TestFixture]
    public class EnumExtensionsTest
    {
        enum Flaggy
        {
            Flag1 = 1,
            Flag2 = 2,
            Flag3 = 4
        }

        [Test]
        public void HasFlagShouldReturnTrueIfTheEnumHasEqualsTheFlag()
        {
            Assert.That(Flaggy.Flag1.HasFlag(Flaggy.Flag1));
            Assert.That(Flaggy.Flag2.HasFlag(Flaggy.Flag2));
            Assert.That(Flaggy.Flag3.HasFlag(Flaggy.Flag3));
        }

        [Test]
        public void HasFlagShouldNotReturnTrueForAnyOfTheOtherFlags()
        {
            Assert.That(Flaggy.Flag1.HasFlag(Flaggy.Flag2), Is.False);
            Assert.That(Flaggy.Flag1.HasFlag(Flaggy.Flag3), Is.False);

            Assert.That(Flaggy.Flag2.HasFlag(Flaggy.Flag1), Is.False);
            Assert.That(Flaggy.Flag2.HasFlag(Flaggy.Flag3), Is.False);

            Assert.That(Flaggy.Flag3.HasFlag(Flaggy.Flag1), Is.False);
            Assert.That(Flaggy.Flag3.HasFlag(Flaggy.Flag2), Is.False);
        }
    }
}