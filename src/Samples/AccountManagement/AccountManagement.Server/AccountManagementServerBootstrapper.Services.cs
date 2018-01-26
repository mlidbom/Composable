using System;
using AccountManagement.Domain;
using AccountManagement.UI;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;

namespace AccountManagement
{
    public static partial class AccountManagementServerBootstrapper
    {
        static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            AccountUIAdapter.GetById(registrar);
            AccountUIAdapter.Register(registrar);
            AccountUIAdapter.ChangeEmail(registrar);
            AccountUIAdapter.ChangePassword(registrar);
            AccountUIAdapter.Login(registrar);

            Account.Repository.Get(registrar);
            Account.Repository.Save(registrar);
            Account.Repository.GetReadonlyCopyOfLatestVersion(registrar);
            Account.Repository.GetReadonlyCopyOfSpecificVersion(registrar);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(registrar);
            EmailToAccountMapper.TryGetAccountByEmail(registrar);
        }
    }
}
