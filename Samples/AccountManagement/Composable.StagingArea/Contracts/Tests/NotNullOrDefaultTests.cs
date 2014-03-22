using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class NotNullOrDefaultTests
    {
        [Test]
        public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
        {
            Assert.Throws<ObjectIsNullException>(() => Contract.Argument<string>(null).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(new object(), null).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments("", null, new object()).NotNullOrDefault());
        }

        [Test]
        public void ThrowsArgumentNullExceptionIfAnyValueIsDefault()
        {
            Assert.That(new MyStructure(), Is.EqualTo(new MyStructure()));
            Assert.That(new MyStructure(), Is.Not.EqualTo(new MyStructure(1)));

            Assert.Throws<ObjectIsDefaultException>(() => Contract.Argument(0).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(new object(), 0).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments("", new object(), new MyStructure()).NotNullOrDefault());
        }

        [Test]
        public void ShouldRun10TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 100; i++)
            {
                Contract.Argument(1).NotNullOrDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }

        private struct MyStructure
        {
            public int Value;

            public MyStructure(int value)
            {
                Value = value;
            }
        }
    }

    public class ObjectIsDefaultException : ContractException
    {
        public ObjectIsDefaultException(string valueName) : base(valueName) {}
    }
}
