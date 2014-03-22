using System;
using System.Linq;

namespace Composable.Contracts
{
    public static class ArgumentTester
    {
        public static InspectionTarget<Guid> NotEmpty(this InspectionTarget<Guid> me)
        {
            if (me.Arguments.Any(parameter => parameter == Guid.Empty))
            {
                throw new ArgumentException();
            }
            return me;
        }

        public static InspectionTarget<TArgument> NotNull<TArgument>(this InspectionTarget<TArgument> me)
            where TArgument : class
        {
            if (me.Arguments.Any(parameter => parameter == null))
            {
                throw new ArgumentNullException();
            }
            return me;
        }
    }
}