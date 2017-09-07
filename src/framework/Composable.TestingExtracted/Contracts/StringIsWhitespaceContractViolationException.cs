namespace Composable.Testing.Contracts
{
    ///<summary>Exception thrown when string is only whitespace when that is not allowed.</summary>
    class StringIsWhitespaceContractViolationException : ContractViolationException
    {
        ///<summary>Standard constructor</summary>
        public StringIsWhitespaceContractViolationException(IInspectedValue badValue) : base(badValue) {}
    }
}
