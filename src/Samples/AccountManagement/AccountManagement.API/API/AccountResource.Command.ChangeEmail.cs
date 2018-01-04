using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable;
using Composable.Contracts;
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
                public ChangeEmail(Guid accountId) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; set; }
                [Required] [Email] public string Email { get; set; }
            }
        }
    }
}