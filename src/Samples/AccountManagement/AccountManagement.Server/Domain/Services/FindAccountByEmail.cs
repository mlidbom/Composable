using AccountManagement.Domain.QueryModels;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class FindAccountByEmail : IFindAccountByEmail
    {
        readonly IAccountManagementDomainDocumentDbReader _querymodels;

        public FindAccountByEmail(IAccountManagementDomainDocumentDbReader querymodels) => _querymodels = querymodels;

        public bool AccountExists(Email email)
        {
            OldContract.Argument(() => email).NotNull();

            return _querymodels.TryGet(email, out EmailToAccountIdQueryModel _);
        }
    }
}
