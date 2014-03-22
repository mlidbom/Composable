using System;

namespace Composable.Contracts
{
    public static class StringInspector
    {
        public static Inspected<string> NotNullOrEmpty(this Inspected<string> me)
        {
            me.NotNull(); //We want the proper exceptions
            return me.Inspect(inspected => inspected != String.Empty,
                badValue => new StringIsEmptyException(badValue.Name));
        }

        public static Inspected<String> NotNullEmptyOrWhiteSpace(this Inspected<String> me)
        {
            me.NotNullOrEmpty(); //We want the proper exceptions
            return me.Inspect(
                inspected => inspected.Trim() != String.Empty,
                badValue => new StringIsWhitespaceException(badValue.Name));
        }
    }
}
