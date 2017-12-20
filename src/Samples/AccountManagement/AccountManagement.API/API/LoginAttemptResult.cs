using System;

namespace AccountManagement.API
{
    public class LoginAttemptResult
    {
        public string AuthenticationToken { get; private set; }
        public bool Succeeded { get; private set; }

        public static LoginAttemptResult Success() => new LoginAttemptResult()
                                                      {
                                                          AuthenticationToken = Guid.NewGuid().ToString(),
                                                          Succeeded = true
                                                      };

        public static LoginAttemptResult Failure() => new LoginAttemptResult()
                                                      {
                                                          Succeeded = false
                                                      };
    }
}
