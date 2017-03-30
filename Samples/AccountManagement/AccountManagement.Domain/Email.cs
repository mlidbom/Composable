using System;
using System.Text.RegularExpressions;
using Composable.DDD;

namespace AccountManagement.Domain
{
    ///<summary>
    /// A small value object that ensures that it is impossible to create an invalid email.
    /// This frees all users of the class from ever having to validated an email.
    /// As long as it is not null it is guaranteed to be valid.
    /// </summary>
    public class Email : ValueObject<Email>
    {
        string Value { get; set; }

        public override string ToString() => Value;

        Email(string emailAddress)
        {
            Validate(emailAddress);
            Value = emailAddress;
        }

        public static bool IsValidEmail(string emailAddress)
        {
            if(string.IsNullOrWhiteSpace(emailAddress))
            {
                return false;
            }

            var regex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$");
            var isMatch = regex.IsMatch(emailAddress);

            if(!isMatch)
            {
                return false;
            }

            if(emailAddress.Contains(".."))
            {
                return false;
            }

            if(emailAddress.Contains("@.") || emailAddress.Contains(".@"))
            {
                return false;
            }
            return true;
        }

        public static Email Parse(string emailAddress) => new Email(emailAddress);

        //Note how all the exceptions contain the invalid email address. Always make sure that exceptions contain the relevant information.
        static void Validate(string emailAddress)
        {
            if(!IsValidEmail(emailAddress))
            {
                throw new InvalidEmailException(emailAddress);
            }
        }
    }

    public class InvalidEmailException : ArgumentException
    {
        internal InvalidEmailException(string message) : base(message ?? "[null]") {}
    }
}
