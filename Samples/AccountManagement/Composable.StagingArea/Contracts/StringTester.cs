using System;
using System.Linq;

namespace Composable.Contracts
{
    public static class StringTester
    {
        public static InspectionTarget<string> NotEmpty(this InspectionTarget<string> me)
        {
            if (me.Arguments.Any(value => value == String.Empty))
            {
                throw new StringIsEmptyException();
            }
            return me;
        }

        public static InspectionTarget<string> NotNullOrEmpty(this InspectionTarget<string> me)
        {
            me.NotNull();//We want the proper exceptions
            if (me.Arguments.Any(value => value == String.Empty))
            {
                throw new StringIsEmptyException();
            }
            return me;
        }

        public static InspectionTarget<String> NotNullEmptyOrWhiteSpace(this InspectionTarget<String> me)
        {
            me.NotNullOrEmpty();//We want the proper exceptions
            if (me.Arguments.Any(value => value.Trim() == String.Empty))
            {
                throw new StringIsWhitespaceException();
            }
            return me;
        }
    }
}