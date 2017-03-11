using System;
// ReSharper disable MemberCanBeInternal

namespace Composable.Contracts
{
    ///<summary>Performs inspections on string instances</summary>
    public static class StringInspector
    {
        ///<summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any expected value is null.</para>
        /// <para>Throws <see cref="StringIsEmptyContractViolationException"/> if any inspected value is an empty string.</para>
        /// </summary>
        public static Inspected<string> NotNullOrEmpty(this Inspected<string> me)
        {
            me.NotNull(); //We want the proper exceptions
            return me.Inspect(inspected => inspected != String.Empty,
                badValue => new StringIsEmptyContractViolationException(badValue));
        }

        ///<summary>
        /// <para>Throws <see cref="ObjectIsNullContractViolationException"/> if any expected value is null.</para>
        /// <para>Throws <see cref="StringIsEmptyContractViolationException"/> if any inspected value is an empty string.</para>
        /// <para>Throws <see cref="StringIsWhitespaceContractViolationException"/> if any inspected value is a string containing only whitespace.</para>
        /// </summary>
        public static Inspected<String> NotNullEmptyOrWhiteSpace(this Inspected<String> me)
        {
            me.NotNullOrEmpty(); //We want the proper exceptions
            return me.Inspect(
                inspected => inspected.Trim() != String.Empty,
                badValue => new StringIsWhitespaceContractViolationException(badValue));
        }
    }
}
