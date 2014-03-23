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

        public bool AccountExists(Email email)
        {
            EmailToAccountMap ignored;
            return _querymodels.TryGet<EmailToAccountMap>(email, out ignored);
        }
    }
}