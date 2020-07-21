using System;
using System.Collections.Generic;
using System.Globalization;
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
        public ConfigurationBasedDuplicateByDimensionsAttribute() : base(CreateDimensions()) {}

        const string NCrunchDuplicateByDimensions = "NCrunchDuplicateByDimensions";
        public static string[] CreateDimensions()
        {
            try
            {
                return File.ReadAllLines(NCrunchDuplicateByDimensions)
                           .Select(@this => @this.Trim())
                           .Where(line => !string.IsNullOrEmpty(line))
                           .Where(line => !line.StartsWith("#", StringComparison.InvariantCulture))
                           .ToArray();
            }
            catch(Exception e)
            {
                return  new[]{ e.ToString() };
            }
        }
    }
}