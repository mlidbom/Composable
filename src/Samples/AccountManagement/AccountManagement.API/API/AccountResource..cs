using AccountManagement.Domain;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource : EntityResource<AccountResource>
    {
        [UsedImplicitly] AccountResource() {}

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            Commands = new AccountResourceCommands(this);
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }
    }
}
