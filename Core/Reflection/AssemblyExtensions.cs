using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Void.Reflection
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<Type> Types(this IEnumerable<Assembly> assemblies)
        {
            Contract.Requires(assemblies!=null);
            return assemblies.SelectMany(assembly => assembly.GetTypes());
        }

        public static bool IsSystemAssembly(this Assembly assembly)
        {
            Contract.Requires(assembly != null);
            return assembly.FullName.StartsWith("System.");
        }
    }
}