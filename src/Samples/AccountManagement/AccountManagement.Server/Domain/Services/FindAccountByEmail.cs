using AccountManagement.Domain.QueryModels;
using Composable.Contracts;
using Composable.Persistence.DocumentDb;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly] class FindAccountByEmail : IFindAccountByEmail
    {
        readonly IDocumentDbReader _querymodels;

        public FindAccountByEmail(IDocumentDbReader querymodels) => _querymodels = querymodels;

        public bool AccountExists(Email email)
        {
            OldContract.Argument(() => email).NotNull();

            return _querymodels.TryGet(email, out EmailToAccountIdQueryModel _);
        }
    }
}
