namespace Composable.Contracts
{
    ///<summary>Exception thrown when guid is empty when that is not allowed.</summary>
    public class GuidIsEmptyContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public GuidIsEmptyContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
