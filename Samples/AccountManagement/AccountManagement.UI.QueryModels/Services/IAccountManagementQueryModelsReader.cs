using System;

namespace AccountManagement.UI.QueryModels.Services
{
    public interface IAccountManagementQueryModelsReader
    {
        AccountQueryModel GetAccount(Guid accountId);
    }
}
