using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using AccountManagement.API;
using AccountManagement.Extensions;
using Composable.System.ComponentModel.DataAnnotations;

namespace AccountManagement.Domain.Passwords
{
    public partial class Password
    {
        public static class Policy
        {
            ///<summary><para>returns a list of the ways in which a specific password fails to fulfill the policy. If the list is empty the password is compliant with the policy.</para> </summary>
            public static IEnumerable<Failures> GetPolicyFailures(string password)
            {
                if(password == null)
                {
                    return new []{Failures.Null}; //Everything else will fail with null reference exception if we don't return here...
                }

                var failures = new List<Failures>();
                //Create a simple extension to keep the code short an expressive and DRY. If AddIf is unclear just hover your pointer over the method and the documentation comment should clear everything up.
                failures.AddIf(password.Length < 4, Failures.ShorterThanFourCharacters);
                failures.AddIf(password.Trim() != password, Failures.BorderedByWhitespace);
                failures.AddIf(password.ToLower() == password, Failures.MissingUppercaseCharacter);
                failures.AddIf(password.ToUpper() == password, Failures.MissingLowerCaseCharacter);
                return failures;
            }

            internal static void AssertPasswordMatchesPolicy(string password)
            {
                var passwordPolicyFailures = GetPolicyFailures(password).ToList();
                if(passwordPolicyFailures.Any())
                {
                    //Don't throw a generic exception or ArgumentException. Throw a specific type that let's clients make use of it easily and safely.
                    throw new PasswordDoesNotMatchPolicyException(passwordPolicyFailures);
                    //Normally we would include the value to make debugging easier but not for passwords since that would be a security issue. We do make sure to include HOW it was invalid.
                }
            }

            [Flags]
            public enum Failures
            {
                Null = 1, //Make sure all values are powers of 2 so that the flags can be combined freely.
                MissingUppercaseCharacter = 2,
                MissingLowerCaseCharacter = 4,
                ShorterThanFourCharacters = 8,
                BorderedByWhitespace = 16
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember)
            {
                var policyFailures = Policy.GetPolicyFailures(password).ToList();
                if (policyFailures.Any())
                {
                    switch (policyFailures.First())
                    {
                        case Policy.Failures.BorderedByWhitespace:
                            yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_BorderedByWhitespace, passwordMember);
                            break;
                        case Policy.Failures.MissingLowerCaseCharacter:
                            yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingLowerCaseCharacter, passwordMember);
                            break;
                        case Policy.Failures.MissingUppercaseCharacter:
                            yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_MissingUpperCaseCharacter, passwordMember);
                            break;
                        case Policy.Failures.ShorterThanFourCharacters:
                            yield return owner.CreateValidationResult(RegisterAccountCommandResources.Password_ShorterThanFourCharacters, passwordMember);
                            break;
                        case Policy.Failures.Null:
                            throw new Exception("Null should have been caught by the Required attribute");
                        default:
                            throw new Exception($"Unknown password failure type {policyFailures.First()}");
                    }
                }
            }
        }
    }
}
