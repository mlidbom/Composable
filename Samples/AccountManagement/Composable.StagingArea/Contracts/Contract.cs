using System;

namespace Composable.Contracts
{
    public static class Contract
    {
        public static void ArgumentNotNull(params object[] arguments)
        {
            Argument(arguments).NotNull();
        }

        public static InspectionTarget<TParameter> Argument<TParameter>(params TParameter[] param)
        {
            return new InspectionTarget<TParameter>(param);
        }

        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<InspectionTarget<TReturnValue>> assert)
        {
            assert(new InspectionTarget<TReturnValue>(returnValue));
            return returnValue;
        }
    }


    public class InspectionTarget<TArgument>
    {
        public readonly TArgument[] Arguments;

        public InspectionTarget(params TArgument[] arguments)
        {
            Arguments = arguments;
        }
    }
}
