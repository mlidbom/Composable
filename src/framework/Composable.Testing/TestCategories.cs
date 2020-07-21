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

        const string NCrunchPersistenceLayers = "NCrunchPersistenceLayers";
        static string[] CreateDimensions(bool excludeMemory)
        {
            try
            {
                if(!File.Exists(NCrunchPersistenceLayers)) throw new Exception($@"
Please create the solutions item file {NCrunchPersistenceLayers} and place each of the persistence layers that you want NCrunch to run tests with on a separate line.
Comment out the once you don't want using a # character at the beginning of the line.
You find the providers in:{typeof(PersistenceLayer).FullName}.
There is also an example file right next to it that you can copy and edit: {NCrunchPersistenceLayers}.example
Once this is done you might also need to rebuild, and/or restart NCrunch for it to notice.
");

                return File.ReadAllLines(NCrunchPersistenceLayers)
                           .Select(@this => @this.Trim())
                           .Where(line => !line.StartsWith("#", StringComparison.InvariantCulture))
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