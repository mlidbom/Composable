using System;
using System.Linq;

namespace Composable.Contracts
{
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
                throw new StringEmptyArgumentException();
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
}