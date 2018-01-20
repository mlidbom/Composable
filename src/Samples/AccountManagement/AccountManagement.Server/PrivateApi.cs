using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Functional;
using Composable.Messaging;

namespace AccountManagement
{
    static class PrivateApi
    {
        internal static class Account
        {
            internal static class Queries
            {
                internal static TryGetByEmailQuery TryGetByEmail(Email email) => new TryGetByEmailQuery(email);

                internal class TryGetByEmailQuery : IQuery<Option<Domain.Account>>
                {
                    public TryGetByEmailQuery(Email accountId)
                    {
                        OldContract.Argument(() => accountId).NotNullOrDefault();
                        Email = accountId;
                    }

                    public Email Email { get; private set; }
                }
            }
        }
    }
}
