using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace UnitTestProject1
{
    [TestClass]
    public class MSTestTestClass
    {
        [TestMethod]
        public void TestMethod1()
        {
        }
    }

    [TestFixture] public class NunitTestClass
    {
        [Test] public void TestMethod()
        {
        }
    }
}
