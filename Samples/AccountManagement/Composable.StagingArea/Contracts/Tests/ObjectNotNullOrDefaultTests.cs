using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ObjectNotNullOrDefaultTests
    {
        [Test]
        public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
        {
            const string theNull = (string)null;
            Assert.Throws<ObjectIsNullException>(() => Contract.ArgumentOptimized(theNull).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.ArgumentsOptimized(new object(), null).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.ArgumentsOptimized("", null, new object()).NotNullOrDefault());
        }

        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            Assert.That(new MyStructure(), Is.EqualTo(new MyStructure()));
            Assert.That(new MyStructure(), Is.Not.EqualTo(new MyStructure(1)));

            Assert.Throws<ObjectIsDefaultException>(() => Contract.ArgumentOptimized(0).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.ArgumentsOptimized(new object(), 0).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.ArgumentsOptimized("", new object(), new MyStructure()).NotNullOrDefault());
        }

        [Test]
        public void ShouldRun10TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 100; i++)
            {
                Contract.ArgumentOptimized(1).NotNullOrDefault();
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
}
