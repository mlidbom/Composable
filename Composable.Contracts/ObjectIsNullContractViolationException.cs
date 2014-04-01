namespace Composable.Contracts
{
    public class ObjectIsNullContractViolationException : ContractViolationException
    {
        public ObjectIsNullContractViolationException(InspectedValue badValue) : base(badValue) {}
    }
}
