using System.Linq;
using NUnit.Framework;
using Void.Linq;

namespace Core.Tests.Linq
{
    [TestFixture]
    public class SeqTests
    {      
        [Test]
        public void CreateShouldEnumerateAllParamsInOrder()
        {
            var oneToTen = 1.To(10);
            Assert.That(Seq.Create(oneToTen.ToArray()), Is.EquivalentTo(oneToTen));
        }
    }
}