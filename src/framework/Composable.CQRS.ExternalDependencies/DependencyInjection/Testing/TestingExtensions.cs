using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection.SimpleInjector;
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


            IDependencyInjectionContainer cloneContainer = sourceContainer switch
            {
                ComposableDependencyInjectionContainer _ => new ComposableDependencyInjectionContainer(sourceContainer.RunMode),
                WindsorDependencyInjectionContainer _ => new WindsorDependencyInjectionContainer(sourceContainer.RunMode),
                SimpleInjectorDependencyInjectionContainer _ => new SimpleInjectorDependencyInjectionContainer(sourceContainer.RunMode),
                _ => throw new ArgumentOutOfRangeException()
            };

            cloneContainer.Register(Singleton.For<IServiceLocator>().CreatedBy(() => cloneContainer.CreateServiceLocator()));

            sourceContainer.RegisteredComponents()
                           .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
