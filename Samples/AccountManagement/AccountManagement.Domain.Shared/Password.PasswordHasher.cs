using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Composable.Contracts;

namespace AccountManagement.Domain.Shared
{
    public partial class Password
    {
        static class PasswordHasher
        {
            public static byte[] HashPassword(byte[] salt, string password) //Extract to a private nested PasswordHasher class if this class gets uncomfortably long.
            {
                ContractTemp.Argument(() => salt, () => password).NotNullOrDefault();

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
