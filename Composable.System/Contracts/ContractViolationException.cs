using System;

namespace Composable.Contracts
{
    /// <summary>
    /// Exceptions raise by the inspectors should inherit this exception type.
    /// </summary>
    class ContractViolationException : Exception
    {
        ///<summary>Standard constructor that will construct a message based on the name and value of the failing member. </summary>
        public ContractViolationException(InspectedValue badValue)
        {
            BadValue = badValue;
        }

        ///<summary>The value that failed inspection.</summary>
        public InspectedValue BadValue { get; private set; }

        ///<summary>Tells which field/property/argument failded inspection and what value it had.</summary>
        public override string Message => $"{BadValue.Type}: {BadValue.Name}";
    }
}
