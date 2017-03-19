using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Composable.Contracts;
using Composable.System.Linq;

namespace Composable {
    static class GeneratedLowLevelInterfaceInspector
    {
        internal static void InspectInterfaces(IEnumerable<Type> types)
        {
            AssertTypesAssemblyHasRequiredAttributes(types);

            Contract.Assert.That(types.All(type => type.IsVisible == false),
                                 @"
The interface implementations generated here are low level implementation interfaces that should not be exposed. They are therefore required to be internal.
If you wish to expose services outside your assembly you should create custom interfaces for your use case and expose those instead.
");
        }

        static readonly HashSet<string> RequiredNames = new HashSet<string>()
                                                        {
                                                            InternalsRequiredToBeVisibleTo.Assembly1,
                                                            InternalsRequiredToBeVisibleTo.Assembly2,
                                                            InternalsRequiredToBeVisibleTo.Assembly3,
                                                            InternalsRequiredToBeVisibleTo.Assembly4
                                                        };
        static void AssertTypesAssemblyHasRequiredAttributes(IEnumerable<Type> types)
        {
            foreach(var assemblyToInspect in types.Select(type => type.Assembly)
                                                  .ToSet())
            {
                var assemblyAttributes = assemblyToInspect.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
                                                          .Cast<InternalsVisibleToAttribute>()
                                                          .Select(attribute => attribute.AssemblyName)
                                                          .ToSet();

                if(assemblyAttributes.Intersect(RequiredNames)
                                     .Count() < RequiredNames.Count)
                {
                    throw new Exception($@"
The assembly: {assemblyToInspect.GetName().Name} is missing attributes required for proxy generation to work correctly. Please ensure that you have these attributes in your assembly:

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(Composable.CQRS.InternalsRequiredToBeVisibleTo.Assembly1)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(Composable.CQRS.InternalsRequiredToBeVisibleTo.Assembly2)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(Composable.CQRS.InternalsRequiredToBeVisibleTo.Assembly3)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(Composable.CQRS.InternalsRequiredToBeVisibleTo.Assembly4)]
");
                }
            }
        }
    }
}