using AccountManagement.Domain.Registration;
using Composable.Contracts;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public partial class Register
            {
                public class RegistrationAttemptResult
                {
                    [JsonConstructor]internal RegistrationAttemptResult(RegistrationAttemptStatus status, AccountResource registeredAccount)
                    {
                        Assert.Argument.Assert(status != RegistrationAttemptStatus.Successful || registeredAccount != null);
                        Status = status;
                        RegisteredAccount = registeredAccount;
                    }

                    public RegistrationAttemptStatus Status { get; private set; }
                    public AccountResource RegisteredAccount { get; private set; }
                }
            }
        }
    }
}
