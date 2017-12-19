using NUnit.Framework;

namespace Composable.Tests
{
    [TestFixture]
    public class EnumExtensionsTest
    {
        enum FlagsEnum
        {
            Flag1 = 1,
            Flag2 = 2,
            Flag3 = 4
        }

        [Test]
        public void HasFlagShouldReturnTrueIfTheEnumHasEqualsTheFlag()
        {
            Assert.That(FlagsEnum.Flag1.HasFlag(FlagsEnum.Flag1));
            Assert.That(FlagsEnum.Flag2.HasFlag(FlagsEnum.Flag2));
            Assert.That(FlagsEnum.Flag3.HasFlag(FlagsEnum.Flag3));
        }

        [Test]
        public void HasFlagShouldNotReturnTrueForAnyOfTheOtherFlags()
        {
            Assert.That(FlagsEnum.Flag1.HasFlag(FlagsEnum.Flag2), Is.False);
            Assert.That(FlagsEnum.Flag1.HasFlag(FlagsEnum.Flag3), Is.False);

            Assert.That(FlagsEnum.Flag2.HasFlag(FlagsEnum.Flag1), Is.False);
            Assert.That(FlagsEnum.Flag2.HasFlag(FlagsEnum.Flag3), Is.False);

            Assert.That(FlagsEnum.Flag3.HasFlag(FlagsEnum.Flag1), Is.False);
            Assert.That(FlagsEnum.Flag3.HasFlag(FlagsEnum.Flag2), Is.False);
        }
    }
}