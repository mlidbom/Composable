using AccountManagement.Domain.QueryModels;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class FindAccountByEmail : IFindAccountByEmail
    {
        readonly IAccountManagementDomainDocumentDbReader _querymodels;

        public FindAccountByEmail(IAccountManagementDomainDocumentDbReader querymodels) => _querymodels = querymodels;

        public void AssertAccountDoesNotExist(Email email)
        {
            OldContract.Argument(() => email).NotNull();

            if(_querymodels.TryGet(email, out EmailToAccountIdQueryModel _))
            {
                throw new DuplicateAccountException(email);
            }
        }
    }
}
