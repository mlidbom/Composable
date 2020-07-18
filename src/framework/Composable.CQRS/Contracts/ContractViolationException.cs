using System;

namespace Composable.Contracts
{
    /// <summary>
    /// Exceptions raise by the inspectors should inherit this exception type.
    /// </summary>
    public class ContractViolationException : Exception
    {
        ///<summary>Standard constructor that will construct a queuedMessageInformation based on the name and value of the failing member. </summary>
        public ContractViolationException(IInspectedValue badValue) => BadValue = badValue;

        ///<summary>The value that failed inspection.</summary>
        public IInspectedValue BadValue { get; private set; }

        ///<summary>Tells which field/property/argument failed inspection and what value it had.</summary>
        public override string Message => $"{BadValue.Type}: {BadValue.Name}";
    }
}
