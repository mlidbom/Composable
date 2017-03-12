using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS
{
    static class RuntimeInstanceGenerator
    {
        static Dictionary<Type, Type> Cache = new Dictionary<Type, Type>();

        static Type InternalGenerate<TSubClassInterface>(string subclassCode, string subClassName) where TSubClassInterface : class
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
                                $"{typeof(TSubClassInterface).Assembly.GetName() .Name}.dll"
                            }
                        };

            var compiler = CodeDomProvider.CreateProvider("CSharp");
            var compilerResults = compiler.CompileAssemblyFromSource(parms, subclassCode);

            if(compilerResults.Errors.HasErrors)
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
        static Func<IWindsorContainer, TSubClassInterface> CreateFactoryMethod<TSubClassInterface>(string subclassCode, string subClassName) where TSubClassInterface : class
        {
            return container =>
                   {
                       // ReSharper disable once UnusedVariable
                       if(Cache.TryGetValue(typeof(TSubClassInterface), out Type cachedSubClass))
                       {
                           return container.Resolve<TSubClassInterface>();
                       }

                       var newlyGeneratedSubclass = InternalGenerate<TSubClassInterface>(subclassCode, subClassName);

                       Cache.Add(typeof(TSubClassInterface), newlyGeneratedSubclass);

                       container.Register(Component.For<TSubClassInterface>()
                                                   .ImplementedBy(newlyGeneratedSubclass)
                                                   .LifestyleScoped());

                       return container.Resolve<TSubClassInterface>();
                   };
        }

        static string SubClassName<TSubClassInterface>() where TSubClassInterface : class, IDocumentDbSession
        {
            var subClassName = $"{nameof(KeyValueStorage.DocumentDbSession)}_generated_implementation_of_{typeof(TSubClassInterface).FullName.Replace(".", "_").Replace("+", "_")}";
            return subClassName;
        }

        internal static class DocumentDbSession
        {
            internal static Func<IWindsorContainer, TSubClassInterface> CreateFactoryMethod<TSubClassInterface>() where TSubClassInterface : class, IDocumentDbSession
            {
                var subClassName = SubClassName<TSubClassInterface>();
                string subclassCode =
                    $@"
public class {subClassName} : 
    {typeof(KeyValueStorage.DocumentDbSession).FullName}, 
    {typeof(TSubClassInterface).FullName.Replace("+", ".")}
{{ 
    public {subClassName}(
        {typeof(IDocumentDb).FullName} backingStore, 
        {typeof(ISingleContextUseGuard).FullName} usageGuard, 
        {typeof(IDocumentDbSessionInterceptor).FullName} interceptor)
            :base(backingStore, usageGuard, interceptor)
    {{
    }}
}}";
                return RuntimeInstanceGenerator.CreateFactoryMethod<TSubClassInterface>(subclassCode, subClassName);
            }
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
