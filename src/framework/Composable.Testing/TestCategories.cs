using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Composable.DependencyInjection;
using NUnit.Framework;

namespace Composable.Testing
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    class PerformanceAttribute : Attribute,
                                 NUnit.Framework.Interfaces.IApplyToTest
    {
        //Add the NUnit category performance
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "Performance");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)] class LongRunningAttribute : Attribute,
                                                                                                    NUnit.Framework.Interfaces.IApplyToTest
    {
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
            => test.Properties.Add("Category", "LongRunning");
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly)]
    public class ConfigurationBasedDuplicateByDimensionsAttribute : NCrunch.Framework.DuplicateByDimensionsAttribute
    {
        public ConfigurationBasedDuplicateByDimensionsAttribute(bool excludeMemory = false) : base(CreateDimensions(excludeMemory)) {}

        const string NCrunchPersistenceProvidersCsv = "NCrunchPersistenceProviders.csv";
        static string[] CreateDimensions(bool excludeMemory)
        {
            try
            {
                if(!File.Exists(NCrunchPersistenceProvidersCsv)) throw new Exception($@"
Please create the solutions item file {NCrunchPersistenceProvidersCsv} and populate it with a comma separated list of the persistence layers that you want NCrunch to run tests with. 
You find the providers in:{typeof(PersistenceLayer).FullName}.
There is also an example file right next to it that you can copy and edit: {NCrunchPersistenceProvidersCsv}.example
Once this is done you might also need to restart NCrunch for it to notice.
");

                return File.ReadAllText(NCrunchPersistenceProvidersCsv)
                           .Split(",")
                           .Select(@this => @this.Trim())
                           .Where(provider => !excludeMemory || provider.ToUpperInvariant() != nameof(PersistenceLayer.Memory).ToUpperInvariant())
                           .ToArray();
            }
            catch(Exception e)
            {
                return  new[]{ e.ToString() };
            }
        }
    }
}