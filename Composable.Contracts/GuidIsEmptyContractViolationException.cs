namespace Composable.Contracts
{
    public class GuidIsEmptyContractViolationException : ContractViolationException
    {
        public GuidIsEmptyContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
