using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Composable.System.Reflection
{
    /// <summary>A collection of extensions to work with <see cref="Assembly"/></summary>
    public static class AssemblyExtensions
    {
        /// <returns>All the types in all <paramref name="assemblies"/></returns>
        public static IEnumerable<Type> Types(this IEnumerable<Assembly> assemblies)
        {
            Contract.Requires(assemblies!=null);
            return assemblies.SelectMany(assembly => assembly.GetTypes());
        }

        ///<returns>true if <paramref name="assembly"/>.FullName starts with the string: "System."</returns>
        public static bool IsSystemAssembly(this Assembly assembly)
        {
            Contract.Requires(assembly != null);
            return assembly.FullName.StartsWith("System.");
        }
    }
}