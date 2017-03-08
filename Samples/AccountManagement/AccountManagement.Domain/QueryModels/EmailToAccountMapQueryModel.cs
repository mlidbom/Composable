using System;
using AccountManagement.Domain.Shared;
using Composable.Contracts;

namespace AccountManagement.Domain.QueryModels
{
    public class EmailToAccountMapQueryModel
    {
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            ContractTemp.Argument(() => email, () => accountId).NotNullOrDefault();

            Email = email;
            AccountId = accountId;
        }

        public Email Email { get; set; }
        public Guid AccountId { get; set; }
    }
}
