#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StructureMap;
using StructureMap.Configuration.DSL;
using StructureMap.Pipeline;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.IO;

#endregion

namespace Composable.DomainEvents
{
    public static class DomainEvent
    {
        private static readonly List<Delegate> ManualSubscribers = new List<Delegate>();

        private static IContainer Container { get; set; } //as before
        private static readonly ISet<Type> Implementors;

        static DomainEvent()
        {
            Implementors = AppDomain.CurrentDomain.BaseDirectory.AsDirectory().GetFiles().WithExtension(".dll", ".exe")
            .Where(assemblyFile => !assemblyFile.Name.StartsWith("System."))
            .Select(assemblyFile =>  Assembly.LoadFrom(assemblyFile.FullName))
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.Implements(typeof(IHandles<>)))
            .ToSet();;

            var illegalImplementations = Implementors.Where(t => !t.IsVisible);
            if (illegalImplementations.Any())
            {
                throw new InternalIHandlesImplementationException(illegalImplementations);
            }
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


        private static IHandles<T> CreateInstance<T>(Type type) where T : IDomainEvent
        {
            return (IHandles<T>) Activator.CreateInstance(type, false);
        }

        private static IEnumerable<IHandles<T>> FetchHandlersOf<T>() where T : IDomainEvent
        {
            return Implementors
                .Where(t => t.Implements<IHandles<T>>())
                .Select(CreateInstance<T>);
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

                FetchHandlersOf<T>().ForEach(handler =>
                             {
                                 handler.Handle(args);
                                 if (handler is IDisposable)
                                 {
                                     ((IDisposable)handler).Dispose();
                                 }
                             });

            ManualSubscribers.OfType<Action<T>>()
                .ForEach(action => action(args));
        }
    }
}