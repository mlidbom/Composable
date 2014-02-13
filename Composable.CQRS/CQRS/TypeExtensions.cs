using System;
using System.Collections.Generic;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.DomainEvents;
using Composable.System.Reflection;

namespace Composable.CQRS
{
    public static class TypeExtensions
    {
        public static IEnumerable<Type> GetAllEventTypesImplemented(this Type me)
        {
            return me.GetAllTypesInheritedOrImplemented().Where(t => t.Implements<IDomainEvent>());
        }

        public static IEnumerable<Type> GetAllAggregateRootEventTypesImplemented(this Type me)
        {
            return me.GetAllTypesInheritedOrImplemented().Where(t => t.Implements<IAggregateRootEvent>());
        }
    }
}