using System;
using System.Linq;
using System.Reflection;
using Composable.System.Linq;
using NUnit.Framework;

namespace ServiceBusApi
{
    [TestFixture]public class TypeExploration
    {
        [Test] public void PlayWithTypeInstances()
        {
            var assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();

            var type1 = assemblyTypes.First();
        }
    }
}
