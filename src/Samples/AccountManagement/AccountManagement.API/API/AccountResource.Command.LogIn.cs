using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable;
using Composable.Messaging.Commands;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public static class LogIn
            {
                [TypeId("FD3A793F-CEDE-4082-B710-1C143133C9E5")]public class UI : TransactionalExactlyOnceDeliveryCommand<LoginAttemptResult>
                {
                    [Required] [Email] public string Email { get; set; }
                    [Required] public string Password { get; set; }
                }

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
