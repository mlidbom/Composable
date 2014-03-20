using System;
using System.Text.RegularExpressions;
using Composable.System;

namespace AccountManagement.Domain.Shared
{
    public struct Email
    {
        private string Value { get; set; }

        override public string ToString()
        {
            return Value;
        }

        private Email(string emailAddress): this()
        {
            Validate(emailAddress);
            Value = emailAddress;
        }

        public static Email Parse(string emailAddress)
        {
            return new Email(emailAddress);
        }

        private static void Validate(string emailAddress)
        {
            if (string.IsNullOrWhiteSpace(emailAddress))
                throw new InvalidEmailException(emailAddress ?? "[null]");

            var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$");
            var isMatch = regex.IsMatch(emailAddress);

            if (!isMatch)
                throw new InvalidEmailException("Email address {0} does not match pattern for email address.".FormatWith(emailAddress));

            if (emailAddress.Contains(".."))
                throw new InvalidEmailException("Double dot ('..') in email address {0} is not allowed.".FormatWith(emailAddress));

            if (emailAddress.Contains("@.") || emailAddress.Contains(".@"))
                throw new InvalidEmailException("Dot with @ in email address {0} is not allowed.".FormatWith(emailAddress));
        }
    }

    public class InvalidEmailException : ArgumentException
    {
        public InvalidEmailException(string message):base(message)
        {
            
        }
    }
}