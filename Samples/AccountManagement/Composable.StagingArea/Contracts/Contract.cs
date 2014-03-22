using System;
using System.Linq;

namespace Composable.Contracts
{
    public static class Contract
    {
        public static void ArgumentNotNull(params object[] arguments)
        {
            Argument(arguments).NotNull();
        }

        public static Argument<TParameter> Argument<TParameter>(params TParameter[] param)
        {
            return new Argument<TParameter>(param);
        }       
    }

    public static class ArgumentTester
    {
        public static Argument<Guid> NotEmpty(this Argument<Guid> me)
        {
            if (me.Arguments.Any(parameter => parameter == Guid.Empty))
            {
                throw new ArgumentException();
            }
            return me;
        }

        public static Argument<String> NotEmpty(this Argument<String> me)
        {
            if (me.Arguments.Any(parameter => parameter == String.Empty))
            {
                throw new StringIsEmptyArgumentException();
            }
            return me;
        }

        public static Argument<String> NotNullOrEmpty(this Argument<String> me)
        {
            me.NotNull();//We want the proper exceptions
            if (me.Arguments.Any(argument => argument == string.Empty))
            {
                throw new ArgumentException();
            }
            return me;
        }

        public static Argument<String> NotNullEmptyOrWhiteSpace(this Argument<String> me)
        {
            me.NotNullOrEmpty();
            if (me.Arguments.Any(argument => argument.Trim() == string.Empty))
            {
                throw new StringIsWhitespaceArgumentException();
            }
            return me;
        }

        public static Argument<TArgument> NotNull<TArgument>(this Argument<TArgument> me)
            where TArgument : class
        {
            if (me.Arguments.Any(parameter => parameter == null))
            {
                throw new ArgumentNullException();
            }
            return me;
        }
    }

    public class Argument<TArgument>
    {
        public readonly TArgument[] Arguments;

        public Argument(TArgument[] arguments)
        {
            Arguments = arguments;
        }
    }
}
