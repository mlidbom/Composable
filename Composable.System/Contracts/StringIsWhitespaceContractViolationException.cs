namespace Composable.Contracts
{
    ///<summary>Exception thrown when string is only whitespace when that is not allowed.</summary>
    class StringIsWhitespaceContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public StringIsWhitespaceContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
