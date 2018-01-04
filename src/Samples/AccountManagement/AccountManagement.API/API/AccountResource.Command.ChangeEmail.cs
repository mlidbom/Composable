using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            [TypeId("746695CC-B3CB-4620-A622-F6831AA4C5F3")]
            public class ChangeEmail : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangeEmail() {}
                internal ChangeEmail(Guid accountId) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; set; }
                [Required] [Email] public string Email { get; set; }
            }
        }
    }
}