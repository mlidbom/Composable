using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging;
using Newtonsoft.Json;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public class ChangePassword : BusApi.Remotable.AtMostOnce.Command, IValidatableObject
            {
                [JsonConstructor]ChangePassword() : base(MessageIdHandling.Reuse) {}
                public ChangePassword(Guid accountId):base(MessageIdHandling.Create) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; set; }
                [Required] public string OldPassword { get; set; }
                [Required] public string NewPassword { get; set; }

                public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Passwords.Password.Policy.Validate(NewPassword, this, () => NewPassword);

                public ChangePassword WithValues(string oldPassword, string newPassword) => new ChangePassword(AccountId)
                                                                                   {
                                                                                       OldPassword = oldPassword,
                                                                                       NewPassword = newPassword
                                                                                   };
            }
        }
    }
}
