namespace Composable.Contracts
{
    public class ObjectIsDefaultContractViolationException : ContractViolationException
    {
        public ObjectIsDefaultContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
