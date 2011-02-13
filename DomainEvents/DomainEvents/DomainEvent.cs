#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.System.IO;
using Composable.System;

#endregion

namespace Composable.DomainEvents
{
    public static class DomainEvent
    {
        private static readonly List<Delegate> ManualSubscribers = new List<Delegate>();
        private static IServiceLocator _locator;
        private static readonly object LockObject = new object();

        public static IServiceLocator ServiceLocator { get
        {
            if(_locator == null)
            {
                throw new Exception("Domain event class has not been initialized. Please make sure to call Init during your application bootstrapping");
            }
            return _locator;
        } }

        public static void Init(IServiceLocator locator)
        {
            InternalInit(locator, allowReinit : false);
        }

        public static void ReInitOnlyUseFromTests(IServiceLocator locator)
        {
            InternalInit(locator, allowReinit: true);
        }


        private static void InternalInit(IServiceLocator locator, bool allowReinit)
        {
            if(locator == null)
            {
                throw new ArgumentNullException("locator");
            }

            lock (LockObject)
            {
                if(!allowReinit && _locator != null)
                {
                    throw new Exception("You may only call init once!");
                }
                _locator = locator;
            }
        }

        private static Type[] GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }catch(Exception)
            {
                //fixme: Swallowing exceptions is not that great....
                return new Type[0];
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
            if (ServiceLocator != null)
            {
                return (IHandles<T>) ServiceLocator.GetInstance(type);
            }
            return (IHandles<T>) Activator.CreateInstance(type, false);
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

                ServiceLocator.GetAllInstances<IHandles<T>>().ForEach(handler =>
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