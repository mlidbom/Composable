using System;
using AccountManagement.Domain;
using Composable.Persistence.DocumentDb;

namespace AccountManagement.UI.QueryModels.Services
{
    interface IAccountManagementUiDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementUiDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementUiDocumentDbBulkReader : IDocumentDbBulkReader { }

    public interface IAccountManagementQueryModelsReader
    {
        AccountQueryModel GetAccount(Guid accountId);
        AccountQueryModel GetAccount(Guid accountId, int version);
        bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account);
    }
}
