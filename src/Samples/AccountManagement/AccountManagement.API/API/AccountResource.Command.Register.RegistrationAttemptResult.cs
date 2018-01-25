using Composable.Contracts;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeMadeStatic.Global Because we want these members to be accessed through the fluent API we don't want to make them static.

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Commands
        {
            public partial class Register
            {
                public class RegistrationAttemptResult
                {
                    [JsonConstructor]internal RegistrationAttemptResult(Statuses status, AccountResource registeredAccount)
                    {
                        Contract.Argument.Assert(status != Statuses.Successful || registeredAccount != null);
                        Status = status;
                        RegisteredAccount = registeredAccount;
                    }

                    public Statuses Status { get; private set; }
                    public AccountResource RegisteredAccount { get; private set; }

                    public enum Statuses
                    {
                        Successful = 1,
                        EmailAlreadyRegistered = 2
                    }
                } 
            }
        }
    }
}
