namespace Composable.Contracts
{
    public class ObjectIsDefaultException : ContractException
    {
        public ObjectIsDefaultException(InspectedValue badValue) : base(badValue) { }
    }
}