using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage;

namespace Composable.Windsor.Testing
{
    public interface IExecuteActionsWhenRewiringForTesting
    {
        IExecuteActionsWhenRewiringForTesting Execute(Action<IWindsorContainer> action);
    }
   
    public static class WindsorTestWiringExtensions
    {
        public static ExecuteActionsWhenRewiringForTests WhenTesting(this IWindsorContainer @this)
        {
            return new ExecuteActionsWhenRewiringForTests(@this);
        }
    }

    public static class RewiringHelperExtensions
    {
        public static IExecuteActionsWhenRewiringForTesting Run(this IExecuteActionsWhenRewiringForTesting @this, Action<IWindsorContainer> action ) => @this.Execute(action);

        public static IExecuteActionsWhenRewiringForTesting ReplaceComponent<TServiceType>(this IExecuteActionsWhenRewiringForTesting @this, string componentName, ComponentRegistration<TServiceType> replacement, string replacementName = null) where TServiceType : class
        {
            return @this.Run(container => container.ReplaceComponent(componentName, replacement, replacementName));
        }

        public static IExecuteActionsWhenRewiringForTesting Register(this IExecuteActionsWhenRewiringForTesting @this, params IRegistration[] registrations)
        {
            return @this.Run(container => container.Register(registrations));
        }

        public static IExecuteActionsWhenRewiringForTesting ReplaceDefault<TServiceType>(this IExecuteActionsWhenRewiringForTesting @this, ComponentRegistration<TServiceType> replacement) where TServiceType : class
        {
            return @this.Run(container => container.ReplaceDefault(replacement));
        }

        public static IExecuteActionsWhenRewiringForTesting RegisterMessageHandlersFromAssemblyContainingType<TAssemblyIdentifier>(this IExecuteActionsWhenRewiringForTesting @this)
        {
            return @this.Run(container => container.RegisterMessageHandlersFromAssemblyContainingType<TAssemblyIdentifier>());
        }

        public static IExecuteActionsWhenRewiringForTesting ReplaceEventStore(this IExecuteActionsWhenRewiringForTesting @this, string name, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: name,
                replacement: Component.For<IEventStore>()
                    .ImplementedBy<InMemoryEventStore>()
                    .LifestyleSingleton(),
                    replacementName: replacementName);
        }

        public static IExecuteActionsWhenRewiringForTesting ReplaceDocumentDb(this IExecuteActionsWhenRewiringForTesting @this, string dbToReplace, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: dbToReplace,
                replacement: Component.For<IDocumentDb>()
                    .ImplementedBy<InMemoryDocumentDb>()
                    .LifestyleSingleton(),
                replacementName: replacementName);
        }
    }



    public class ExecuteActionsWhenRewiringForTests : IExecuteActionsWhenRewiringForTesting
    {
        private readonly IWindsorContainer Container;

        public ExecuteActionsWhenRewiringForTests(IWindsorContainer container)
        {
            Container = container;
        }

        public IExecuteActionsWhenRewiringForTesting Execute(Action<IWindsorContainer> action)
        {
            Container.Register(
                Component.For<IConfigureWiringForTests>()
                         .Instance(new LambdaBasedTestRewirer(() => action(Container)))
                         .LifestyleSingleton());
            return this;
        }

        private class LambdaBasedTestRewirer : IConfigureWiringForTests
        {
            private readonly Action _action;

            public LambdaBasedTestRewirer(Action action)
            {
                _action = action;
            }

            public void ConfigureWiringForTesting()
            {
                _action();
            }
        }
    }
}
