using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Linq;

namespace Composable.CQRS.RuntimeTypeGeneration
{
    static partial class RuntimeInstanceGenerator
    {
        static readonly Dictionary<string, Type> Cache = new Dictionary<string, Type>();

        static Type InternalGenerate(string subclassCode, string subClassName, Assembly subclassAssembly)
        {
            var parms = new CompilerParameters
                        {
                            GenerateExecutable = false,
                            GenerateInMemory = true,
                            IncludeDebugInformation = false,
                            ReferencedAssemblies =
                            {
                                $"{typeof(AssemblyHandleComposableCqrs).Assembly.GetName() .Name}.dll",
                                $"{typeof(AssemblyHandleComposableCore).Assembly.GetName() .Name}.dll",
                                $"{subclassAssembly.GetName() .Name}.dll"
                            }
                        };

            var compiler = CodeDomProvider.CreateProvider("CSharp");
            var compilerResults = compiler.CompileAssemblyFromSource(parms, subclassCode);

            if (compilerResults.Errors.HasErrors)
            {
                throw new SubclassGenerationException(subclassCode, compilerResults);
            }

            var generatedSubClass = compilerResults.CompiledAssembly.GetType(subClassName);
            return generatedSubClass;
        }

        //Todo: Optimize startup time for applications requesting many types. Letting us do this is why this returns a Func
        //Maybe lazily build an assembly for all requested subclasses when getting the first request for an instance.
        //Or maybe push off creating the class as a background worker etc.
        //Or maybe switch to using castle.dynamicproxy somehow.
        static Func<IWindsorContainer, object> CreateFactoryMethod(string subclassCode, string subClassName, params Type[] serviceTypes)
        {
            return container =>
                   {
                       // ReSharper disable once UnusedVariable
                       if (Cache.TryGetValue(subClassName, out Type cachedSubClass))
                       {
                           return container.Resolve(serviceTypes[0]);
                       }

                       var newlyGeneratedSubclass = InternalGenerate(subclassCode, subClassName, serviceTypes[0].Assembly);

                       Cache.Add(subClassName, newlyGeneratedSubclass);

                       container.Register(Component.For(serviceTypes)
                                                   .ImplementedBy(newlyGeneratedSubclass)
                                                   .LifestyleScoped());

                       return container.Resolve(serviceTypes[0]);
                   };
        }

        static string SubClassName<TSubClassInterface>()
        {
            var subClassName = $"{nameof(KeyValueStorage.DocumentDbSession)}_generated_implementation_of_{typeof(TSubClassInterface).FullName.Replace(".", "_").Replace("+", "_")}";
            return subClassName;
        }
    }

    class SubclassGenerationException : Exception
    {
        public SubclassGenerationException(string subclassCode, CompilerResults compilerResults) : base($@"
Compiler error:
{compilerResults.Errors[0]},

For code: 

{subclassCode}
") {}
    }
}
