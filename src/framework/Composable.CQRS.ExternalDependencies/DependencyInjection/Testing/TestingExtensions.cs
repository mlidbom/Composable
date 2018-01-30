using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection.SimpleInjectorImplementation;
using Composable.DependencyInjection.Windsor;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
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

            var cloneContainer = DependencyInjectionContainer.Create();

            sourceContainer.RegisteredComponents()
                           .Where(component => TypesThatAreFacadesForTheContainer.None(facadeForTheContainer => component.ServiceTypes.Contains(facadeForTheContainer)))
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}
