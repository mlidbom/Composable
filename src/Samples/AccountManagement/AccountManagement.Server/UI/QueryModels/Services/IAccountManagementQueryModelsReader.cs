using Composable.Persistence.DocumentDb;

namespace AccountManagement.UI.QueryModels.Services
{
    interface IAccountManagementUiDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementUiDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementUiDocumentDbBulkReader : IDocumentDbBulkReader { }
}
