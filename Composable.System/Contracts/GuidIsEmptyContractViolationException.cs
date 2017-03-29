namespace Composable.Contracts
{
    ///<summary>Exception thrown when guid is empty when that is not allowed.</summary>
    class GuidIsEmptyContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        internal GuidIsEmptyContractViolationException(IInspectedValue badValue) : base(badValue) {}
    }
}
