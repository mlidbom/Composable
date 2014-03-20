using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AccountManagement.Domain.Shared
{
    /// <summary>
    /// A struct is often the best choice for a value object. 
    /// All those argument tests to ensure that things are not null are no longer needed. Nice.
    /// 
    /// Note how all the business logic of a secure password is encapsulated and the instance is immutable after being created.
    /// </summary>
    public struct Password
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

        public Password(string password) : this()
        {
            if(string.IsNullOrWhiteSpace(password))
            {
                //Don't throw a generic exception or ArgumentException. Throw a specific type that let's clients make use of it easily and safely.
                throw new InvalidPasswordException();
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
    }

    public class InvalidPasswordException : ArgumentException
    {
        
    }
}