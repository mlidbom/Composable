using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection.SimpleInjector;
using Composable.DependencyInjection.Windsor;
using Composable.SystemCE.LinqCE;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {
       static readonly IReadOnlyList<Type> TypesThatAreFacadesForTheContainer = EnumerableCE.OfTypes<IDependencyInjectionContainer, IServiceLocator, SimpleInjectorDependencyInjectionContainer, WindsorDependencyInjectionContainer>()
                                                         .ToList();

        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = (IDependencyInjectionContainer)@this;


            IDependencyInjectionContainer cloneContainer = sourceContainer switch
            {
#pragma warning disable CA2000 // Dispose objects before losing scope: Review: OK-ish. We dispose the container byr registering its created serviceLocator in the container. It will dispose the container when disposed.
                ComposableDependencyInjectionContainer _ => new ComposableDependencyInjectionContainer(sourceContainer.RunMode),
                WindsorDependencyInjectionContainer _ => new WindsorDependencyInjectionContainer(sourceContainer.RunMode),
                SimpleInjectorDependencyInjectionContainer _ => new SimpleInjectorDependencyInjectionContainer(sourceContainer.RunMode),
                _ => throw new ArgumentOutOfRangeException()
#pragma warning restore CA2000 // Dispose objects before losing scope
            };

            cloneContainer.Register(Singleton.For<IServiceLocator>().CreatedBy(() => cloneContainer.CreateServiceLocator()));

            sourceContainer.RegisteredComponents()
                           .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
