using System;
using System.Security.Cryptography;
using System.Text;

namespace AccountManagement.Domain.Shared
{
    public partial class Password
    {
        private static class PasswordHasher
        {
            public static byte[] HashPassword(byte[] salt, string password) //Extract to a private nested PasswordHasher class if this class gets uncomfortably long.
            {
                var encodedPassword = Encoding.Unicode.GetBytes(password);
                var saltedPassword = new byte[salt.Length + encodedPassword.Length];
                Array.Copy(salt, 0, saltedPassword, 0, salt.Length);
                Array.Copy(encodedPassword, 0, saltedPassword, salt.Length, encodedPassword.Length);
                using(var algorithm = SHA256.Create())
                {
                    return algorithm.ComputeHash(saltedPassword);
                }
            }
        }
    }
}
