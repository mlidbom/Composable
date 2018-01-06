using AccountManagement.Domain;
using Composable;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    [TypeId("510E648A-0160-4F86-B1C6-7C63E786AD77")]
    public partial class AccountResource : EntityResource<AccountResource>
    {
        [UsedImplicitly] AccountResource() {}

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            CommandsCollections = new CommandsCollection(this);
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }
    }
}
