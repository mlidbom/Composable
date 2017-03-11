using System;
using AccountManagement.Domain.Shared;
using Composable.Contracts;

namespace AccountManagement.Domain.QueryModels
{
    class EmailToAccountMapQueryModel
    {
        public EmailToAccountMapQueryModel(Email email, Guid accountId)
        {
            Contract.Argument(() => email, () => accountId).NotNullOrDefault();

            Email = email;
            AccountId = accountId;
        }

        Email Email { get; set; }
        Guid AccountId { get; set; }
    }
}
