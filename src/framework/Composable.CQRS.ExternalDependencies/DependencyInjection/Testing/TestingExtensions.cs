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
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this)
        {
            if(@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.DatabasePool)
            {
                MasterDbConnection.UseConnection(action: _ => {}); //evaluate lazy here in order to not pollute profiler timings of component resolution or registering.
            }

            var globalBusStateTracker = new GlobalBusStateTracker();
            var endpointId = new EndpointId(Guid.NewGuid());
            var configuration = new EndpointConfiguration(endpointId.ToString());

            var typeMapper = new TypeMapper();
            var registry = new MessageHandlerRegistry(typeMapper);
            EndpointBuilder.DefaultWiring(globalBusStateTracker, @this, endpointId, configuration, typeMapper, registry);
        }

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
