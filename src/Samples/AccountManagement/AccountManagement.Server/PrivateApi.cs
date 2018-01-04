using AccountManagement.Domain;
using Composable;
using Composable.Contracts;
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

                [TypeId("D57E5445-4D18-4A0F-AFE9-05B8BED78251")]
                internal class TryGetByEmailQuery : IQuery<TryGetEntityQueryResult<Domain.Account>>
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
