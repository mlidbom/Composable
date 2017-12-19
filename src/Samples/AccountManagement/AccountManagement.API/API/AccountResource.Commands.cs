using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging.Commands;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public CommandsCollection CommandsCollections { get; private set; }
        public class CommandsCollection
        {
            [JsonProperty] Guid _accountId;

            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource) => _accountId = accountResource.Id;

            public ChangeEmailUICommand ChangeEmail(string email) => new ChangeEmailUICommand()
                                                                   {
                                                                       Email = email,
                                                                       AccountId = _accountId
                                                                   };

            public AccountResource.Command.ChangePassword.UI ChangePassword(string oldPassword, string newPassword) => new AccountResource.Command.ChangePassword.UI()
                                                                                                   {
                                                                                                       AccountId = _accountId,
                                                                                                       OldPassword = oldPassword,
                                                                                                       NewPassword = newPassword
                                                                                                   };
        }



        public class ChangeEmailUICommand : DomainCommand
        {
            [Required] [EntityId] public Guid AccountId { get; set; }
            [Required] [Email] public string Email { get; set; }
        }
    }
}
