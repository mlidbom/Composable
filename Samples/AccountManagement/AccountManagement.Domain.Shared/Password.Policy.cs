using System;
using System.Collections.Generic;
using System.Linq;
using AccountManagement.Domain.Shared.Extensions;
using Composable.System.Linq;

namespace AccountManagement.Domain.Shared
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
                    return Seq.Create(Failures.Null); //Everything else will fail with null reference exception if we don't return here...
                }

                var failures = new List<Failures>();
                //Create a simple extension to keep the code short an expressive and DRY. If AddIf is unclear just hover your pointer over the method and the documentation comment should clear everything up.
                failures.AddIf(password.Length < 4, Failures.ShorterThanFourCharacters);
                failures.AddIf(password.Trim() != password, Failures.BorderedByWhitespace);
                failures.AddIf(password.ToLower() == password, Failures.MissingUppercaseCharacter);
                failures.AddIf(password.ToUpper() == password, Failures.MissingLowerCaseCharacter);
                return failures;
            }

            public static void AssertPasswordMatchesPolicy(string password)
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
                Null = 1,//Make sure all values are powers of 2 so that the flags can be combined freely.
                MissingUppercaseCharacter = 2,
                MissingLowerCaseCharacter = 4,
                ShorterThanFourCharacters = 8,
                BorderedByWhitespace = 16
            }
        }
    }
}
