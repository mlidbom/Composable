using System;
using AccountManagement.Domain;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels
{
    public class EmailToAccountMapQueryModel
    {
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Email = email;
            AccountId = accountId;
        }

        Email Email { [UsedImplicitly] get; set; }
        internal Guid AccountId { get; private set; }
    }
}
