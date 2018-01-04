using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable;
using Composable.Messaging.Commands;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            [TypeId("43922809-0404-4392-B754-38928C905EBF")]
            public class ChangePassword : TransactionalExactlyOnceDeliveryCommand, IValidatableObject
            {
                public ChangePassword(Guid accountId) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; private set; }
                [Required] public string OldPassword { get; set; }
                [Required] public string NewPassword { get; set; }

                public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Password.Validate(NewPassword, this, () => NewPassword);
            }
        }
    }
}
