namespace Composable.Contracts
{
    public class ObjectIsNullException : ContractException
    {
        public ObjectIsNullException(InspectedValue badValue) : base(badValue) { }
    }
}
