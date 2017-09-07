namespace Composable.Testing.Contracts
{
    ///<summary>Exception thrown when object is null when that is not allowed.</summary>
    class ObjectIsDefaultContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        internal ObjectIsDefaultContractViolationException(IInspectedValue badValue) : base(badValue) {}
    }
}
