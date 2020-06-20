using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Windsor;
using Composable.System.Linq;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {
       static readonly IReadOnlyList<Type> TypesThatAreFacadesForTheContainer = Seq.OfTypes<IDependencyInjectionContainer, IServiceLocator, SimpleInjectorDependencyInjectionContainer, WindsorDependencyInjectionContainer>()
                                                         .ToList();

        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = (IDependencyInjectionContainer)@this;

#pragma warning disable IDE0067 //Review OK-ish: The created servicelocator that is return is actually the cloneContainer cast to another interface.
            var cloneContainer = DependencyInjectionContainer.Create();
#pragma warning restore IDE0067 // Dispose objects before losing scope

            sourceContainer.RegisteredComponents()
                           .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
