using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AccountManagement.Domain.Shared.Extensions;
using Composable.System.Linq;

namespace AccountManagement.Domain.Shared
{
    /// <summary>
    /// Note how all the business logic of a secure password is encapsulated and the instance is immutable after being created.
    /// </summary>
    public class Password
    {
        public byte[] HashedPassword { get; private set; }
        public byte[] Salt { get; private set; }
        
        /// <summary>
        /// Returns true if the supplied password parameter is the same string that was used to create this password.
        /// In other words if the user should succeed in logging in using that password.
        /// </summary>
        public bool IsCorrectPassword(string password)
        {
            return HashedPassword.SequenceEqual(HashPassword(Salt, password));
        }

        public Password(string password)
        {
            var passwordPolicyFailures = Policy.GetPolicyFailures(password).ToList();
            if(passwordPolicyFailures.Any())
            {
                //Don't throw a generic exception or ArgumentException. Throw a specific type that let's clients make use of it easily and safely.
                throw new PasswordDoesNotMatchPolicyException(passwordPolicyFailures);//Normally we would include the value to make debugging easier but not for passwords since that would be a security issue. We do make sure to include HOW it was invalid.
            }

            Salt = Guid.NewGuid().ToByteArray();
            HashedPassword = HashPassword(salt: Salt, password: password);
        }

        private static byte[] HashPassword(byte[] salt, string password)
        {
            var encodedPassword = Encoding.Unicode.GetBytes(password);
            var saltedPassword = new byte[salt.Length + encodedPassword.Length];
            Array.Copy(salt, 0, saltedPassword, 0, salt.Length);
            Array.Copy(encodedPassword, 0, saltedPassword, salt.Length, encodedPassword.Length);
            using (var algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(saltedPassword);
            }
        }

        public static class Policy
        {
            public static IEnumerable<Failures> GetPolicyFailures(string password)
            {                
                if(password == null)
                {
                    return Seq.Create(Failures.Null);//Everything else will fail with null reference exception if we don't return here...
                }

                var failures = new List<Failures>();
                //Create a simple extension to keep the code short an expressive and DRY. If AddIf is unclear just hover your pointer over the method and the documentation comment should clear everything up.
                failures.AddIf(password.Length < 4, Failures.ShorterThanFourCharacters);
                failures.AddIf(password.Trim() != password, Failures.ContainsWhitespace);
                failures.AddIf(password.ToLower() == password, Failures.MissingUppercaseCharacter);
                failures.AddIf(password.ToUpper() == password, Failures.MissingLowerCaseCharacter);                
                return failures;
            }

            private static void AddIf(List<Failures> failures, bool condition, Failures toAdd)
            {
                if(condition)
                {
                    failures.Add(toAdd);
                }
            }

            [Flags]
            public enum Failures
            {
                Null = 1,
                MissingUppercaseCharacter = 2,
                MissingLowerCaseCharacter = 4,
                ShorterThanFourCharacters = 8,
                ContainsWhitespace = 16
            }
        }

        public void AssertIsCorrectPassword(string attemptedPassword)
        {
            if(!IsCorrectPassword(attemptedPassword))
            {
                throw new WrongPasswordException();
            }
        }
    }
}