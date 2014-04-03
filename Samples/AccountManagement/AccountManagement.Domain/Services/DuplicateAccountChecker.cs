using AccountManagement.Domain.QueryModels;
using AccountManagement.Domain.Shared;
using Composable.Contracts;

namespace AccountManagement.Domain.Services
{
    internal class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        private readonly IAccountManagementDomainQueryModelSession _querymodels;

        public DuplicateAccountChecker(IAccountManagementDomainQueryModelSession querymodels)
        {
            _querymodels = querymodels;
        }

        public void AssertAccountDoesNotExist(Email email)
        {
            Contract.Argument(() => email).NotNull();

            EmailToAccountMapQueryModel ignored;
            if(_querymodels.TryGet(email, out ignored))
            {
                throw new DuplicateAccountException(email);
            }
        }
    }
}
