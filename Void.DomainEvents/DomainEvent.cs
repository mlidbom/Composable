#region usings

using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.ServiceLocatorAdapter;

#endregion

namespace Void.DomainEvents
{
    public static class DomainEvent
    {
        [ThreadStatic] //so that each thread has its own callbacks
            private static List<Delegate> actions;

        private static IServiceLocator Container { get; set; } //as before

        static DomainEvent()
        {
            var registry = new Registry();
            registry.Scan(
                scanner =>
                    {
                        scanner.AssembliesFromApplicationBaseDirectory();
                        scanner.ConnectImplementationsToTypesClosing(typeof (IHandles<>));
                    }
                );

            var cont = new Container(registry);

            Container = new StructureMapServiceLocator(cont);
        }

        /// <summary>
        /// Registers a callback for the given domain event.
        /// This callback will only be called by events thrown on the thread that registers the handler.
        /// Should only be used for testing. Implement <see cref="IHandles{TEvent}"/> for normal usage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public static void Register<T>(Action<T> callback) where T : IDomainEvent
        {
            if (actions == null)
                actions = new List<Delegate>();

            actions.Add(callback);
        }


        /// <summary>Clears callbacks passed to Register on the current thread.</summary>
        public static void ClearCallbacks()
        {
            actions = null;
        }


        /// <summary>
        /// Raises the given domain event
        /// All implementors of <see cref="IHandles{T}"/> will be instantiated and called
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        public static void Raise<T>(T args) where T : IDomainEvent
        {
            if (Container != null)
                foreach (var handler in Container.GetAllInstances<IHandles<T>>())
                    handler.Handle(args);


            if (actions != null)
                foreach (var action in actions)
                    if (action is Action<T>)
                        ((Action<T>) action)(args);
        }
    }
}