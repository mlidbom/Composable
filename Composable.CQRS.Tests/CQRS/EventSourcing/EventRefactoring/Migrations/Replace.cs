using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.EventRefactoring.Migrations;
using Composable.System.Linq;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    public class Replace<TEvent> : EventMigration<IRootEvent>
    {
        private readonly IEnumerable<Type> _replaceWith;

        public static Replace<TEvent> With<T1>() => new Replace<TEvent>(Seq.OfTypes<T1>());
        public static Replace<TEvent> With<T1, T2>() => new Replace<TEvent>(Seq.OfTypes<T1, T2>());
        public static Replace<TEvent> With<T1, T2, T3>() => new Replace<TEvent>(Seq.OfTypes<T1, T2, T3>());

        private Replace(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

        public override ISingleAggregateInstanceEventMigrator CreateMigrator() { return new Migrator(_replaceWith); }

        private class Migrator : ISingleAggregateInstanceEventMigrator
        {
            private readonly IEnumerable<Type> _replaceWith;

            public Migrator(IEnumerable<Type> replaceWith) { _replaceWith = replaceWith; }

            public void MigrateEvent(IAggregateRootEvent @event, IEventModifier modifier)
            {
                if (@event.GetType() == typeof(TEvent))
                {
                    modifier.Replace(_replaceWith.Select(Activator.CreateInstance).Cast<AggregateRootEvent>().ToList());
                }
            }
        }
    }
}