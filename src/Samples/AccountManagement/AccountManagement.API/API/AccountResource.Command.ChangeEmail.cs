using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public class ChangeEmail : ExactlyOnceCommand
            {
                [UsedImplicitly] ChangeEmail() {}
                internal ChangeEmail(Guid accountId) => AccountId = accountId;

                [Required] [EntityId] public Guid AccountId { get; set; }
                [Required] [Email] public string Email { get; set; }

                public ChangeEmail WithEmail(string email) => new ChangeEmail(AccountId)
                                                              {
                                                                  Email = email
                                                              };
            }
        }
    }
}