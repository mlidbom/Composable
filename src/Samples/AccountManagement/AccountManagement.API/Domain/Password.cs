using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using AccountManagement.API;
using Composable.System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AccountManagement.Domain
{
    /// <summary>
    /// Note how all the business logic of a secure password is encapsulated and the instance is immutable after being created.
    /// </summary>
    public partial class Password
    {
        public byte[] HashedPassword { get; private set; }
        public byte[] Salt { get; private set; }

        [UsedImplicitly] Password() { }

        public Password(string password)
        {
            Policy.AssertPasswordMatchesPolicy(password); //Use a nested class to make the policy easily discoverable while keeping this class short and expressive.
            Salt = Guid.NewGuid().ToByteArray();
            HashedPassword = PasswordHasher.HashPassword(salt: Salt, password: password);
        }

        /// <summary>
        /// Returns true if the supplied password parameter is the same string that was used to create this password.
        /// In other words if the user should succeed in logging in using that password.
        /// </summary>
        public bool IsCorrectPassword(string password) => HashedPassword.SequenceEqual(PasswordHasher.HashPassword(Salt, password));

        public void AssertIsCorrectPassword(string attemptedPassword)
        {
            if(!IsCorrectPassword(attemptedPassword))
            {
                throw new WrongPasswordException();
            }
        }

        public static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember)
        {
            var policyFailures = Domain.Password.Policy.GetPolicyFailures(password).ToList();
            if (policyFailures.Any())
            {
                switch (policyFailures.First())
                {
                    case Domain.Password.Policy.Failures.BorderedByWhitespace:
                        yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_BorderedByWhitespace, passwordMember);
                        break;
                    case Domain.Password.Policy.Failures.MissingLowerCaseCharacter:
                        yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter, passwordMember);
                        break;
                    case Domain.Password.Policy.Failures.MissingUppercaseCharacter:
                        yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter, passwordMember);
                        break;
                    case Domain.Password.Policy.Failures.ShorterThanFourCharacters:
                        yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_ShorterThanFourCharacters, passwordMember);
                        break;
                    case Domain.Password.Policy.Failures.Null:
                        throw new Exception("Null should have been caught by the Required attribute");
                    default:
                        throw new Exception($"Unknown password failure type {policyFailures.First()}");
                }
            }
        }
    }
}
