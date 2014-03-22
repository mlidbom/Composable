namespace Composable.Contracts
{
    public class InspectionTarget<TArgument>
    {
        public readonly TArgument[] Arguments;

        public InspectionTarget(params TArgument[] arguments)
        {
            Arguments = arguments;
        }
    }
}