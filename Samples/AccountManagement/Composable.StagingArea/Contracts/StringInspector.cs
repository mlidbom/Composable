using System;

namespace Composable.Contracts
{
    public static class StringInspector
    {
        ///<summary>
        /// <para>Throws <see cref="ObjectIsNullException"/> if any expected value is null.</para>
        /// <para>Throws <see cref="StringIsEmptyException"/> if any inspected value is an empty string.</para>
        /// <para>Throws <see cref="StringIsWhitespaceException"/> if any inspected value is a string containing only whitespace.</para>
        /// </summary>
        public static Inspected<string> NotNullOrEmpty(this Inspected<string> me)
        {
            me.NotNull(); //We want the proper exceptions
            return me.Inspect(inspected => inspected != String.Empty,
                badValue => new StringIsEmptyException(badValue));
        }

        public static Inspected<String> NotNullEmptyOrWhiteSpace(this Inspected<String> me)
        {
            me.NotNullOrEmpty(); //We want the proper exceptions
            return me.Inspect(
                inspected => inspected.Trim() != String.Empty,
                badValue => new StringIsWhitespaceException(badValue));
        }
    }
}
