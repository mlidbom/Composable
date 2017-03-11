namespace Composable.Contracts
{
    ///<summary>Exception thrown when object is null and that is not allowed.</summary>
    class ObjectIsNullContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public ObjectIsNullContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
