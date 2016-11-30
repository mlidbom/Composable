#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Threading;
using Composable.System.Linq;

#endregion

namespace Composable.DomainEvents
{
    internal static class DomainEvent
    {
        private static readonly ThreadLocal<List<Delegate>> ManualSubscribersStorage =
            new ThreadLocal<List<Delegate>>(() => new List<Delegate>());

        private static List<Delegate> ManualSubscribers
        {
            get
            {
                Contract.Ensures(Contract.Result<List<Delegate>>() != null);
                Contract.Assume(ManualSubscribersStorage.Value != null);
                return ManualSubscribersStorage.Value;
            }
        }

        private static readonly object LockObject = new object();

        [ContractInvariantMethod]
        private static void Invariants()
        {
            Contract.Invariant(ManualSubscribers != null);
        }
       
        /// <summary>
        /// Registers a callback for the given domain event.
        /// Should only be used for testing. Implement <see cref="IHandles{TEvent}"/> for normal usage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="callback"></param>
        [Obsolete("Only use if you are really sure you know what you are doing. Any use except to wrap synchronous calls in a using block may behave erratically with for instance the asp.net threading model...")]
        public static IDisposable RegisterShortTermSynchronousListener<T>(Action<T> callback) where T : IDomainEvent
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
        [ContractVerification(false)]
        public static void Raise<T>(T args) where T : IDomainEvent
        {
            Contract.Requires(args != null);
            //THis is called in tight loops occationally. Do not waste cycles on linq

            for(var index = 0; index < ManualSubscribers.Count; index++)
            {
                if(ManualSubscribers[index] is Action<T>)
                {
                    ((Action<T>)ManualSubscribers[index])(args);
                }
            }
        }
    }
}