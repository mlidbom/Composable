using AccountManagement.Domain.QueryModels;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        readonly IAccountManagementDomainDocumentDbReader _querymodels;

        public DuplicateAccountChecker(IAccountManagementDomainDocumentDbReader querymodels) => _querymodels = querymodels;

        public void AssertAccountDoesNotExist(Email email)
        {
            Contract.Argument(() => email).NotNull();

            if(_querymodels.TryGet(email, out EmailToAccountMapQueryModel _))
            {
                throw new DuplicateAccountException(email);
            }
        }
    }
}
