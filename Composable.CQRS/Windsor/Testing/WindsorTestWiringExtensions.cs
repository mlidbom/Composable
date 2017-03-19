using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage;
using Composable.Persistence.KeyValueStorage;

// ReSharper disable UnusedMethodReturnValue.Global

namespace Composable.Windsor.Testing
{
    static class RewiringHelperExtensions
    {
        internal static IExecuteActionsWhenRewiringForTesting ReplaceEventStore(this IExecuteActionsWhenRewiringForTesting @this, string name, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: name,
#pragma warning disable 618
                replacement: Component.For<IEventStore>()
#pragma warning restore 618
                    .ImplementedBy<InMemoryEventStore>()
                    .LifestyleSingleton(),
                    replacementName: replacementName);
        }

        public static IExecuteActionsWhenRewiringForTesting ReplaceDocumentDb(this IExecuteActionsWhenRewiringForTesting @this, string dbToReplace, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: dbToReplace,
#pragma warning disable 618
                replacement: Component.For<IDocumentDb>()
#pragma warning restore 618
                    .ImplementedBy<InMemoryDocumentDb>()
                    .LifestyleSingleton(),
                replacementName: replacementName);
        }

        static IExecuteActionsWhenRewiringForTesting Run(this IExecuteActionsWhenRewiringForTesting @this, Action<IWindsorContainer> action) => @this.Execute(action);

        static IExecuteActionsWhenRewiringForTesting ReplaceComponent<TServiceType>(this IExecuteActionsWhenRewiringForTesting @this, string componentName, ComponentRegistration<TServiceType> replacement, string replacementName = null) where TServiceType : class
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
    }



    class ExecuteActionsWhenRewiringForTests : IExecuteActionsWhenRewiringForTesting
    {
        readonly IWindsorContainer _container;

        public ExecuteActionsWhenRewiringForTests(IWindsorContainer container) => _container = container;

        public IExecuteActionsWhenRewiringForTesting Execute(Action<IWindsorContainer> action)
        {
            _container.Register(
                                Component.For<IConfigureWiringForTests>()
                                         .Instance(new LambdaBasedTestRewirer(() => action(_container)))
                                         .Named(Guid.NewGuid().ToString())
                                         .LifestyleSingleton());
            return this;
        }

        class LambdaBasedTestRewirer : IConfigureWiringForTests
        {
            readonly Action _action;

            public LambdaBasedTestRewirer(Action action) => _action = action;

            public void ConfigureWiringForTesting()
            {
                _action();
            }
        }
    }

    public interface IExecuteActionsWhenRewiringForTesting
    {
        IExecuteActionsWhenRewiringForTesting Execute(Action<IWindsorContainer> action);
    }

    static class WindsorTestWiringExtensions
    {
        public static IExecuteActionsWhenRewiringForTesting WhenTesting(this IWindsorContainer @this) => new ExecuteActionsWhenRewiringForTests(@this);
    }

}
