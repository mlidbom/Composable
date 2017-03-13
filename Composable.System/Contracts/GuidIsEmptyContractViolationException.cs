namespace Composable.Contracts
{
    ///<summary>Exception thrown when guid is empty when that is not allowed.</summary>
    class GuidIsEmptyContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public GuidIsEmptyContractViolationException(IInspectedValue badValue) : base(badValue) {}
    }
}
