using System;
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
            public class ChangeEmail : BusApi.Remotable.AtMostOnce.Command
            {
                [UsedImplicitly] public ChangeEmail():base(DeduplicationIdHandling.Reuse) {}
                internal ChangeEmail(Guid accountId):base(DeduplicationIdHandling.Create) => AccountId = accountId;

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