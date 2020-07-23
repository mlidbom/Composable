using System.Linq;
using Composable.SystemCE.LinqCE;
using NUnit.Framework;

namespace Composable.Tests.Linq
{
    [TestFixture]
    public class SeqTests
    {
        [Test]
        public void CreateShouldEnumerateAllParamsInOrder()
        {
            var oneToTen = 1.Through(10);
            Assert.That(EnumerableCE.Create(oneToTen.ToArray()), Is.EquivalentTo(oneToTen));
        }
    }
}