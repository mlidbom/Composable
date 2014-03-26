namespace Composable.Contracts
{
    public class StringIsWhitespaceContractViolationException : ContractViolationException
    {
        public StringIsWhitespaceContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
