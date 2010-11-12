#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;
using Void.Linq;
using Void.Reflection;

#endregion

namespace Void.DomainEvents
{
    public static class DomainEvent
    {
        private static readonly List<Delegate> ManualSubscribers = new List<Delegate>();

        private static IContainer Container { get; set; } //as before

        static DomainEvent()
        {
            var registry = new Registry();
            var assemblies = new List<Assembly>();
            var excludedAssemblies = Seq.Create("System.", "Microsoft.");
            registry.Scan(
                scanner =>
                    {
                        scanner.AssembliesFromApplicationBaseDirectory(
                            assembly =>
                                {
                                    if (excludedAssemblies.None(excluded => assembly.FullName.StartsWith(excluded)))
                                    {
                                        assemblies.Add(assembly);
                                        return true;
                                    }
                                    return false;
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
            
            //AssertconfigurationIsValid creates instances that are apt to implement idisposable and need to be disposed.
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            
            Container = container;
        }

        /// <summary>
        /// Registers a callback for the given domain event.
        /// Should only be used for testing. Implement <see cref="IHandles{TEvent}"/> for normal usage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        public static IDisposable Register<T>(Action<T> callback) where T : IDomainEvent
        {
            ManualSubscribers.Add(callback);
            return new RemoveRegistration(callback);
        }

        private class RemoveRegistration : IDisposable
        {
            private readonly Delegate _callbackToRemove;

            public RemoveRegistration(Delegate callback)
            {
                _callbackToRemove = callback;
            }

            public void Dispose()
            {
                ManualSubscribers.Remove(_callbackToRemove);
            }
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
                .ForEach(handler =>
                             {
                                 handler.Handle(args);
                                 if (handler is IDisposable)
                                 {
                                     ((IDisposable) handler).Dispose();
                                 }
                             });

            ManualSubscribers.OfType<Action<T>>()
                .ForEach(action => action(args));
        }
    }
}