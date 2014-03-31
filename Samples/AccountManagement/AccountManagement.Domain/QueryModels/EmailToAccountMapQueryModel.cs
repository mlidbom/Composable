using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain.QueryModels
{
    public class EmailToAccountMapQueryModel
    {
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Email = email;
            AccountId = accountId;
        }

        public Email Email { get; set; }
        public Guid AccountId { get; set; }
    }
}
