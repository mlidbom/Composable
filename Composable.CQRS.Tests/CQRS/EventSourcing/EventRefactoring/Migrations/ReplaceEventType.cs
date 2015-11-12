using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class ReplaceEventType<TEvent> : EventMigration
    {
        private readonly IEnumerable<Type> _replaceWith;

        public static ReplaceEventType<TEvent> With<T1>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1>());
        public static ReplaceEventType<TEvent> With<T1, T2>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1, T2>());
        public static ReplaceEventType<TEvent> With<T1, T2, T3>() => new ReplaceEventType<TEvent>(Seq.OfTypes<T1, T2, T3>());

        public ReplaceEventType(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

        public override void InspectEvent(IAggregateRootEvent @event, IEventModifier modifier)
        {
            if(@event.GetType() == typeof(TEvent))
            {
                modifier.Replace(_replaceWith.Select(Activator.CreateInstance).Cast<IAggregateRootEvent>().ToList());
            }
        }
    }
}