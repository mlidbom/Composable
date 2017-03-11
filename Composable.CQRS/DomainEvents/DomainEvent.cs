#region usings

using System;
using System.Collections.Generic;
using System.Threading;
using Composable.Contracts;
using Composable.DomainEvents;

#endregion

namespace Composable.CQRS.DomainEvents
{
    //Review:mlidbo: REMOVE
    static class DomainEvent
    {
        static readonly ThreadLocal<List<Delegate>> ManualSubscribersStorage =
            new ThreadLocal<List<Delegate>>(() => new List<Delegate>());

        static List<Delegate> ManualSubscribers
        {
            get
            {
                Contract.Assert(ManualSubscribersStorage.Value != null);
                return ManualSubscribersStorage.Value;
            }
        }

        [Obsolete("Only use if you are really sure you know what you are doing. Any use except to wrap synchronous calls in a using block may behave erratically with for instance the asp.net threading model...")]
        public static IDisposable RegisterShortTermSynchronousListener<T>(Action<T> callback) where T : IDomainEvent
        {
            ManualSubscribers.Add(callback);
            return new RemoveRegistration(callback);
        }

        class RemoveRegistration : IDisposable
        {
            readonly Delegate _callbackToRemove;

            public RemoveRegistration(Delegate callback)
            {
                _callbackToRemove = callback;
            }

            public void Dispose()
            {
                ManualSubscribers.Remove(_callbackToRemove);
            }
        }

        public static void Raise<T>(T args) where T : IDomainEvent
        {
            if(args == null)
            {
                throw new ArgumentNullException();
            }

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