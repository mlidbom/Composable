using System;
using Composable.System;

namespace Composable.Contracts
{
    public class ContractViolationException : Exception
    {
        public ContractViolationException(InspectedValue badValue)
        {
            BadValue = badValue;
        }

        public InspectedValue BadValue { get; private set; }

        override public string Message { get { return "{0}: {1}".FormatWith(BadValue.Type, BadValue.Name); } }
    }
}
