using AccountManagement.Domain.QueryModels;
using AccountManagement.Domain.Shared;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        readonly IAccountManagementDomainDocumentDbSession _querymodels;

        public DuplicateAccountChecker(IAccountManagementDomainDocumentDbSession querymodels) => _querymodels = querymodels;

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
