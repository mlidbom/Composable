using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.Persistence.KeyValueStorage;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.RuntimeTypeGeneration
{
    class DocumentDbFactoryMethods<TSessionInterface,
                                   TUpdaterInterface,
                                   TReaderInterface>
        where TSessionInterface : class, IDocumentDbSession
        where TUpdaterInterface : class, IDocumentDbUpdater
        where TReaderInterface : class, IDocumentDbReader
    {
        readonly Func<IWindsorContainer, object> _internalFactory;
        public DocumentDbFactoryMethods(Func<IWindsorContainer, object> internalFactory) => _internalFactory = internalFactory;

        internal TSessionInterface CreateSession(IWindsorContainer container) => (TSessionInterface)_internalFactory(container);
        internal TReaderInterface CreateReader(IWindsorContainer container) => (TReaderInterface)_internalFactory(container);
        internal TUpdaterInterface CreateUpdater(IWindsorContainer container) => (TUpdaterInterface)_internalFactory(container);
    }

    static class RuntimeInstanceGenerator
    {
        static readonly HashSet<Type> ProhibitedBuiltInTypes = Seq.OfTypes<IDocumentDbSession, IDocumentDbReader, IDocumentDbUpdater>()
                                                                  .ToSet();
        internal static class DocumentDb
        {
            internal static DocumentDbFactoryMethods<TSessionInterface, TUpdaterInterface, TReaderInterface> CreateFactoryMethod<
                TSessionInterface,
                TUpdaterInterface,
                TReaderInterface>()
                where TSessionInterface : class, IDocumentDbSession
                where TUpdaterInterface : class, IDocumentDbUpdater
                where TReaderInterface : class, IDocumentDbReader
            {

                var requestedServiceInterfaces = Seq.OfTypes<TSessionInterface, TUpdaterInterface, TReaderInterface>().ToArray();

                if (requestedServiceInterfaces.ToSet().Intersect(ProhibitedBuiltInTypes).Any())
                {
                    throw new ArgumentException("The service interfaces you supply must inherit from the built in interfaces. You are not allowed to supply the built in interfaces.");
                }

                var subClassName = SubClassName<TSessionInterface>();
                string subclassCode =
                    $@"
public class {subClassName} : 
    {typeof(KeyValueStorage.DocumentDbSession).FullName}, 
    {typeof(TSessionInterface).FullName.Replace("+", ".")},
    {typeof(TUpdaterInterface).FullName.Replace("+", ".")},
    {typeof(TReaderInterface).FullName.Replace("+", ".")}
{{ 
    public {subClassName}(
        {typeof(IDocumentDb).FullName} backingStore, 
        {typeof(ISingleContextUseGuard).FullName} usageGuard, 
        {typeof(IDocumentDbSessionInterceptor).FullName} interceptor)
            :base(backingStore, usageGuard, interceptor)
    {{
    }}
}}";
                var internalFactory = RuntimeInstanceGenerator.CreateFactoryMethod(subclassCode, subClassName, requestedServiceInterfaces);
                return new DocumentDbFactoryMethods<TSessionInterface, TUpdaterInterface, TReaderInterface>(internalFactory);
            }
        }


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

        static string SubClassName<TSubClassInterface>() where TSubClassInterface : class, IDocumentDbSession
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
