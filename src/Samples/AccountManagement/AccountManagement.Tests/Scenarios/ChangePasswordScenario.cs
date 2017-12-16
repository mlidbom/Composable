using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    public class ChangePasswordScenario
    {
        readonly IServiceLocator _serviceLocator;
        readonly IServiceBus _clientBus;

        public string OldPassword;
        public readonly string NewPasswordAsString = TestData.Password.CreateValidPasswordString();
        public Password NewPassword;
        public AccountResource Account { get; private set; }

        public ChangePasswordScenario(IServiceLocator serviceLocator, IServiceBus clientBus)
        {
            _serviceLocator = serviceLocator;
            _clientBus = clientBus;
            NewPassword = new Password(NewPasswordAsString);
            var registerAccountScenario = new RegisterAccountScenario(clientBus);
            Account = registerAccountScenario.Execute();
            OldPassword = registerAccountScenario.Command.Password;
        }

        public void Execute()
        {
            _serviceLocator.ExecuteTransaction(() => _serviceLocator.Use<IAccountRepository>(repo => repo.Get(Account.Id)
                                                                                              .ChangePassword(oldPassword: OldPassword,
                                                                                                              newPassword: NewPassword)));

            Account = _clientBus.Query(AccountApi.Start.Queries.AccountById(Account.Id));
        }
    }
}
