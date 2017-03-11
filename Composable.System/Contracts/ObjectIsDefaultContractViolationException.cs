namespace Composable.Contracts
{
    ///<summary>Exception thrown when object is null when that is not allowed.</summary>
    class ObjectIsDefaultContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public ObjectIsDefaultContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
