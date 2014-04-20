using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.UI.QueryModels.Services
{
    public interface IAccountManagementQueryModelsReader
    {
        AccountQueryModel GetAccount(Guid accountId);
        bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account);
    }
}
