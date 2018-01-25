using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging.Commands;
// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Commands
        {
            public class LogIn : ExactlyOnceCommand<LogIn.LoginAttemptResult>
            {
                    [Required] [Email] public string Email { get; set; }
                    [Required] public string Password { get; set; }

                    internal LogIn WithValues(string email, string password) => new LogIn
                                                                           {
                                                                               Email = email,
                                                                               Password = password
                                                                           };

                public class LoginAttemptResult
                {
                    public string AuthenticationToken { get; private set; }
                    public bool Succeeded { get; private set; }

                    public static LoginAttemptResult Success(string authenticationToken) => new LoginAttemptResult()
                                                                                            {
                                                                                                AuthenticationToken = authenticationToken,
                                                                                                Succeeded = true
                                                                                            };

                    public static LoginAttemptResult Failure() => new LoginAttemptResult()
                                                                  {
                                                                      Succeeded = false
                                                                  };
                }
            }
        }
    }
}
