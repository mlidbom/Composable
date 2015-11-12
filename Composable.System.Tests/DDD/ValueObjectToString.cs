#region usings

using System;
using Composable.DDD;
using NUnit.Framework;

#endregion

namespace Composable.Tests.DDD
{
    [TestFixture]
    public class ValueObjectToString
    {
        public class Root : ValueObject<Root>
        {
            public string Name { get; set; }
            public Branch Branch1 { get; set; }
            public Branch Branch2 { get; set; }
        }

        public class Branch
        {
            public string Name { get; set; }
            public Leaf Leaf1 { get; set; }
            public Leaf Leaf2 { get; set; }
        }

        public class Leaf
        {
            public string Name { get; set; }
        }

        [Test]
        public void ReturnsHirerarchicalDescriptionOfData()
        {
            var description = new Root
                                  {
                                      Name = "RootName",
                                      Branch1 = new Branch
                                                    {
                                                        Name = "Branch1Name",
                                                        Leaf1 = new Leaf { Name = "Leaf1Name" },
                                                        Leaf2 = new Leaf { Name = "Leaf2Name" },
                                                    },
                                      Branch2 = new Branch
                                                    {
                                                        Name = "Branch1Name",
                                                        Leaf1 = new Leaf { Name = "Leaf1Name" },
                                                        Leaf2 = new Leaf { Name = "Leaf2Name" },
                                                    }
                                  }.ToString();
            Assert.That(description, Is.EqualTo(
                @"Composable.Tests.DDD.ValueObjectToString+Root:{""Name"":""RootName"",""Branch1"":{""Name"":""Branch1Name"",""Leaf1"":{""Name"":""Leaf1Name""},""Leaf2"":{""Name"":""Leaf2Name""}},""Branch2"":{""Name"":""Branch1Name"",""Leaf1"":{""Name"":""Leaf1Name""},""Leaf2"":{""Name"":""Leaf2Name""}}}".Replace("\r\n","\n")
                .Replace("\n",Environment.NewLine)));//Hack to get things working regardless of checkout line endings
        }
    }
}