using System;
using System.Collections.Generic;
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
        public partial class CommandsCollection
        {
            [JsonProperty] Guid _accountId;

            [UsedImplicitly] CommandsCollection() {}

            public CommandsCollection(AccountResource accountResource) => _accountId = accountResource.Id;

            public ChangeEmailUICommand ChangeEmail(string email) => new ChangeEmailUICommand()
                                                                   {
                                                                       Email = email,
                                                                       AccountId = _accountId
                                                                   };

            public ChangePasswordUICommand ChangePassword(string oldPassword, string newPassword) => new ChangePasswordUICommand()
                                                                                                   {
                                                                                                       AccountId = _accountId,
                                                                                                       OldPassword = oldPassword,
                                                                                                       NewPassword = newPassword
                                                                                                   };
        }

        public class ChangePasswordUICommand : DomainCommand, IValidatableObject
        {
            [Required] [EntityId] public Guid AccountId { get; set; }
            [Required] public string OldPassword { get; set; }
            [Required] public string NewPassword { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(NewPassword, this, () => NewPassword);
        }

        public class ChangeEmailUICommand : DomainCommand
        {
            [Required] [EntityId] public Guid AccountId { get; set; }
            [Required] [Email] public string Email { get; set; }
        }
    }
}
