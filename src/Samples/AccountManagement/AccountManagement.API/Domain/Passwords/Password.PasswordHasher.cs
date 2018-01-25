using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Composable.Contracts;

namespace AccountManagement.Domain.Passwords
{
    public partial class HashedPassword
    {
        //Use a private nested class to the Password class short and readable while keeping the hashing logic private.
        static class PasswordHasher
        {
            public static byte[] HashPassword(byte[] salt, string password) //Extract to a private nested PasswordHasher class if this class gets uncomfortably long.
            {
                OldContract.Argument(() => salt, () => password).NotNullOrDefault();

                var encodedPassword = Encoding.Unicode.GetBytes(password);
                var saltedPassword = salt.Concat(encodedPassword).ToArray();

                using(var algorithm = SHA256.Create())
                {
                    return algorithm.ComputeHash(saltedPassword);
                }
            }
        }
    }
}
