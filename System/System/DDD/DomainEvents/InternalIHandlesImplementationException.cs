#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;

#endregion

namespace Composable.DomainEvents
{
    public class InternalIHandlesImplementationException : Exception
    {
        public InternalIHandlesImplementationException(IEnumerable<Type> illegalImplementations) : base(
            "These types Implement IHandles but are not public \n{0}"
                .FormatWith(
                    illegalImplementations.Select(t => t.AssemblyQualifiedName).Join(Environment.NewLine)))
        {
        }
    }
}