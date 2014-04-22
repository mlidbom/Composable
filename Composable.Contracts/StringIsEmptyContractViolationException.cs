namespace Composable.Contracts
{
    ///<summary>Exception thrown when string is empty and that is not allowed.</summary>
    public class StringIsEmptyContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public StringIsEmptyContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
