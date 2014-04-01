using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountManagement.Domain.Shared
{
    public class PasswordDoesNotMatchPolicyException : ArgumentException
    {
        public PasswordDoesNotMatchPolicyException(IEnumerable<Password.Policy.Failures> passwordPolicyFailures) : base(BuildMessage(passwordPolicyFailures))
        {
            Failures = passwordPolicyFailures;
        }

        public IEnumerable<Password.Policy.Failures> Failures { get; private set; }

        private static string BuildMessage(IEnumerable<Password.Policy.Failures> passwordPolicyFailures)
        {
            return string.Join(",", passwordPolicyFailures.Select(failure => failure.ToString()));
        }
    }
}
