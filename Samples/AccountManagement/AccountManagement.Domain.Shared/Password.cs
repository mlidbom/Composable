using System;
using System.Security.Cryptography;
using System.Text;

namespace AccountManagement.Domain.Shared
{
    /// <summary>
    /// Note how all the business logic of a secure password is encapsulated.
    /// </summary>
    public struct Password
    {
        public byte[] HashedPassword { get; private set; }
        public byte[] Salt { get; private set; }
        
        public bool IsCorrectPassword(string password)
        {
            return HashedPassword == HashPassword(Salt, password);
        }

        public Password(string password) : this()
        {
            if(string.IsNullOrWhiteSpace(password))
            {
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

    public class InvalidPasswordException : Exception
    {
        
    }
}