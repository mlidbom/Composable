using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public class ChangePassword : MessageTypes.Remotable.AtMostOnce.AtMostOnceHypermediaCommand, IValidatableObject
            {
                [UsedImplicitly] public ChangePassword() : base(DeduplicationIdHandling.Reuse) {}
                public ChangePassword(Guid accountId):base(DeduplicationIdHandling.Create) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; set; }
                [Required] public string OldPassword { get; set; } = string.Empty;
                [Required] public string NewPassword { get; set; } = string.Empty;

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
