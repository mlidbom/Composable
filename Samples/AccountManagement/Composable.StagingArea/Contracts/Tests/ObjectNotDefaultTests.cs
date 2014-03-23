using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ObjectNotDefaultTests
    {
        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var myStructure = new MyStructure();
            // ReSharper disable ConvertToConstant.Local
            var zero = 0;
            // ReSharper restore ConvertToConstant.Local

            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Argument(zero).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Arguments(zero).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Argument(myStructure).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Arguments(myStructure).NotDefault());

            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => zero).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => myStructure).NotDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => myStructure).NotDefault());
        }

        [Test]
        public void ShouldRun10TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 100; i++)
            {
                Contract.Optimized.Argument(1).NotDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }

        private struct MyStructure
        {           
        }
    }
}
