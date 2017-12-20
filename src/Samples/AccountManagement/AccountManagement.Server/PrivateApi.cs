using System;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Messaging;

namespace AccountManagement
{
    static class PrivateApi
    {
        internal class TryGetAccountByEmailQuery : IQuery<Account>
        {
            public TryGetAccountByEmailQuery(Email accountId)
            {
                OldContract.Argument(() => accountId).NotNullOrDefault();
                Email = accountId;
            }

            public Email Email { get; private set; }
        }
    }
}
