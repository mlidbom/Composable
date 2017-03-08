using AccountManagement.Domain.QueryModels;
using AccountManagement.Domain.Shared;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        readonly IAccountManagementDomainQueryModelSession _querymodels;

        public DuplicateAccountChecker(IAccountManagementDomainQueryModelSession querymodels)
        {
            _querymodels = querymodels;
        }

        public void AssertAccountDoesNotExist(Email email)
        {
            ContractTemp.Argument(() => email).NotNull();

            EmailToAccountMapQueryModel ignored;
            if(_querymodels.TryGet(email, out ignored))
            {
                throw new DuplicateAccountException(email);
            }
        }
    }
}
