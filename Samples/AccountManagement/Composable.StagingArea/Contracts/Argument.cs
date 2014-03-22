namespace Composable.Contracts
{
    public class Argument<TArgument>
    {
        public readonly TArgument[] Arguments;

        public Argument(TArgument[] arguments)
        {
            Arguments = arguments;
        }
    }
}