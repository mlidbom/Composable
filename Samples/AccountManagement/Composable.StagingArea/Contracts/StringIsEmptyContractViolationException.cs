namespace Composable.Contracts
{
    public class StringIsEmptyContractViolationException : ContractViolationException
    {
        public StringIsEmptyContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
