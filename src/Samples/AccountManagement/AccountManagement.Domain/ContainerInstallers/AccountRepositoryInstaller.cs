﻿using AccountManagement.Domain.Services;
using Composable.DependencyInjection;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class AccountRepositoryInstaller
    {
        internal static void SetupContainer(IDependencyInjectionContainer container)
        {
            container.Register(
                Component.For<IAccountRepository>().ImplementedBy<AccountRepository>().LifestyleScoped()
                );
        }
    }
}