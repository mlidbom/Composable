using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using Composable.Persistence.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.KeyValueStorage
{
    public partial class DocumentDbSession
    {
        internal class SubClassGenerator
        {
            static Dictionary<Type, Type> Cache = new Dictionary<Type, Type>();
            public static Type GenerateSubClass<TSubClassInterface>() where TSubClassInterface : IDocumentDbSession
            {
                if(Cache.TryGetValue(typeof(TSubClassInterface), out Type generatedSubclass))
                {
                    return generatedSubclass;
                }

                var subClassName = $"{nameof(DocumentDbSession)}_generated_implementation_of_{typeof(TSubClassInterface).FullName.Replace(".", "_") .Replace("+", "_")}";
                string subclassCode =
                    $@"
public class {subClassName} : 
    {typeof(DocumentDbSession).FullName}, 
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

                var parms = new CompilerParameters
                            {
                                GenerateExecutable = false,
                                GenerateInMemory = true,
                                IncludeDebugInformation = false,
                                ReferencedAssemblies =
                                {
                                    $"{typeof(SubClassGenerator).Assembly.GetName().Name}.dll",
                                    $"{typeof(AssemblyHandleComposableCore).Assembly.GetName().Name}.dll",
                                    $"{typeof(TSubClassInterface).Assembly.GetName().Name}.dll"
                                }
                            };

                var compiler = CodeDomProvider.CreateProvider("CSharp");
                var compilerResults = compiler.CompileAssemblyFromSource(parms, subclassCode);

                if(compilerResults.Errors.HasErrors)
                {
                    throw new SubclassGenerationException(subclassCode, compilerResults);
                }

                var generateSubClass = compilerResults.CompiledAssembly.GetType(subClassName);
                Cache.Add(typeof(TSubClassInterface), generateSubClass);
                return generateSubClass;
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
")
        {
        }
    }
}
