using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace AccountManagement.Domain.Passwords
{
    public partial class Password
    {
        Password(string password)
        {
            Policy.AssertPasswordMatchesPolicy(password);
            StringValue = password;
        }

        public string StringValue { get; set; }

        public HashedPassword Hash() => new HashedPassword(StringValue);

        public static Password Parse(string password) => new Password(password);

        public static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember) => Policy.Validate(password, owner, passwordMember);
    }
}
