// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public partial class LogIn
            {
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
