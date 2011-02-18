using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using Composable.System;
using Composable.System.Linq;
using System.Linq;

namespace Composable.CQRS
{
    public class DuplicateHandlersException : Exception
    {
        public DuplicateHandlersException(Type requestedType, IEnumerable<object> instances):base(CreateMessage(requestedType, instances))
        {
            Contract.Requires(requestedType!=null && instances!=null);
        }

        private static String CreateMessage(Type type, IEnumerable<object> instances)
        {
            Contract.Requires(type != null && instances!=null);
            var message = "Multiple handlers registered for {0}:".FormatWith(type);
            return instances.Select(i => i.GetType()).Aggregate(message, (aggregate, instance) => aggregate + Environment.NewLine + instance.FullName);
        }
    }
}