using System.Diagnostics.CodeAnalysis;
using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using Composable.DDD;
using Newtonsoft.Json;

namespace AccountManagement.API
{
    public partial class AccountResource : Entity<AccountResource>
    {
#pragma warning disable IDE0051 // Remove unused private members
        [JsonConstructor]AccountResource(Email email, Password password, CommandsCollection commands)
#pragma warning restore IDE0051 // Remove unused private members
        {
            Email = email;
            Password = password;
            Commands = commands;
        }

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            Commands = new CommandsCollection(this);
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }

        public CommandsCollection Commands { get; private set; }
    }
}
