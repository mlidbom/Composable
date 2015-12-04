using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.KeyValueStorage;

namespace Composable.Windsor.Testing
{

    public static class RewiringHelperExtensions
    {
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

    
}
