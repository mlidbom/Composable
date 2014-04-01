using System;

namespace Composable.Contracts
{
    /// <summary>
    /// Exceptions raise by the inspectors should inherit this exception type.
    /// </summary>
    public class ContractViolationException : Exception
    {
        public ContractViolationException(InspectedValue badValue)
        {
            BadValue = badValue;
        }

        public InspectedValue BadValue { get; private set; }

        override public string Message { get { return string.Format("{0}: {1}", BadValue.Type, BadValue.Name); } }
    }
}
