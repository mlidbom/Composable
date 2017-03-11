using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.UI.QueryModels
{
    public class EmailToAccountMapQueryModel
    {
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Email = email;
            AccountId = accountId;
        }

        Email Email { get; set; }
        internal Guid AccountId { get; private set; }
    }
}
