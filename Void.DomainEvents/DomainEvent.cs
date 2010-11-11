#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Configuration.DSL;
using Void.Reflection;
using Void.Linq;

#endregion

namespace Void.DomainEvents
{
    public static class DomainEvent
    {
        [ThreadStatic] //so that each thread has its own callbacks
        private static readonly List<Delegate> ManualSubscribers = new List<Delegate>();

        private static IContainer Container { get; set; } //as before

        static DomainEvent()
        {
            var registry = new Registry();
            var assemblies = new List<Assembly>();
            registry.Scan(
                scanner =>
                    {
                        scanner.AssembliesFromApplicationBaseDirectory(assembly =>
                                                                           {
                                                                               assemblies.Add(assembly);
                                                                               return true;
                                                                           }
                            );
                        scanner.ConnectImplementationsToTypesClosing(typeof (IHandles<>));
                    }
                );


            var container = new Container(registry);

            var illegalImplementations = assemblies.Types()
                .Where(t => t.Implements(typeof (IHandles<>)) && !t.IsVisible);

            if (illegalImplementations.Any())
            {
                throw new InternalIHandlesImplementationException(illegalImplementations);
            }

            container.AssertConfigurationIsValid();
            Container = container;
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
            ManualSubscribers.Add(callback);
        }


        /// <summary>Clears callbacks passed to Register on the current thread.</summary>
        public static void ClearCallbacks()
        {
            ManualSubscribers.Clear();
        }


        /// <summary>
        /// Raises the given domain event
        /// All implementors of <see cref="IHandles{T}"/> will be instantiated and called.
        /// All registered <see cref="Action{T}"/> instances will be invoked
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        public static void Raise<T>(T args) where T : IDomainEvent
        {
                Container.GetAllInstances<IHandles<T>>()
                    .ForEach(handler => handler.Handle(args));

                ManualSubscribers.OfType<Action<T>>()
                    .ForEach(action => action(args));
        }
    }
}