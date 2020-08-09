using System;
using System.Reflection;
using System.Reflection.Emit;
using Composable.Messaging;
using Composable.SystemCE.ThreadingCE.ResourceAccess;

#pragma warning disable CA1810 // Initialize reference type static fields inline

namespace Composable.SystemCE.ReflectionCE.EmitCE
{
    public static class AssemblyBuilderCE
    {
        internal static readonly IThreadShared<ModuleBuilder> Module;

        static AssemblyBuilderCE()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{nameof(AssemblyBuilderCE)}_dynamic_assembly"), AssemblyBuilderAccess.Run);
            Module = ThreadShared.WithDefaultTimeout(assembly.DefineDynamicModule(assembly.GetName().Name.NotNull()));
        }
    }

    public static class TypeBuilderCE
    {
        const MethodAttributes PropertyAccessorAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        public static (FieldInfo, PropertyInfo) ImplementProperty(this TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField($"_{propertyName}",
                                                                propertyType,
                                                                FieldAttributes.Private);

            var propertyBuilder = typeBuilder.DefineProperty(name: propertyName,
                                                             attributes: PropertyAttributes.HasDefault,
                                                             returnType: propertyType,
                                                             parameterTypes: null);

            typeBuilder.ImplementGetMethod(propertyBuilder, fieldBuilder);
            typeBuilder.ImplementSetMethod(propertyBuilder, fieldBuilder);

            return (fieldBuilder, propertyBuilder);
        }

        public static void ImplementConstructor(this TypeBuilder typeBuilder, FieldInfo field)
        {
            Type[] constructorArgs = {field.FieldType};
            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public,
                                                                     CallingConventions.Standard,
                                                                     constructorArgs);
            // Generate IL for the method. The constructor stores its argument in the private field.
            var ilGenerator = constructorBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, field);
            ilGenerator.Emit(OpCodes.Ret);
        }

        static void ImplementSetMethod(this TypeBuilder typeBuilder, PropertyBuilder propertyBuilder, FieldBuilder fieldBuilder)
        {
            var setMethodBuilder = typeBuilder.DefineMethod(name: $"set_{propertyBuilder.Name}",
                                                            attributes: PropertyAccessorAttributes,
                                                            returnType: null,
                                                            parameterTypes: new[] {propertyBuilder.PropertyType});

            var ilGenerator = setMethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        static void ImplementGetMethod(this TypeBuilder typeBuilder, PropertyBuilder propertyBuilder, FieldBuilder fieldBuilder)
        {
            var getMethodBuilder = typeBuilder.DefineMethod(name: $"get_{propertyBuilder.Name}",
                                                            attributes: PropertyAccessorAttributes | MethodAttributes.Virtual,
                                                            returnType: propertyBuilder.PropertyType,
                                                            parameterTypes: null);

            var ilGenerator = getMethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getMethodBuilder);
        }
    }
}
