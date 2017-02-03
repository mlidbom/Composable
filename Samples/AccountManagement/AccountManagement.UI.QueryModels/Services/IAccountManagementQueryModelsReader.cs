using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.UI.QueryModels.Services
{
    public interface IAccountManagementQueryModelsReader
    {
        AccountQueryModel GetAccount(Guid accountId);
        AccountQueryModel GetAccount(Guid accountId, int version);
        bool TryGetAccountByEmail(Email accountEmail, out AccountQueryModel account);
    }
}
