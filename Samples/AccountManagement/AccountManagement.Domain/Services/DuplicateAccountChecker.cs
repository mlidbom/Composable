using AccountManagement.Domain.QueryModels;
using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain.Services
{
    public class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        private readonly IAccountManagementDomainQueryModelSession _querymodels;

        public DuplicateAccountChecker(IAccountManagementDomainQueryModelSession querymodels)
        {
            _querymodels = querymodels;
        }

        public void AssertAccountDoesNotExist(Email email)
        {
            EmailToAccountMap ignored;
            if(_querymodels.TryGet(email, out ignored))
            {
                throw new DuplicateAccountException(email);
            }
        }
    }
}
