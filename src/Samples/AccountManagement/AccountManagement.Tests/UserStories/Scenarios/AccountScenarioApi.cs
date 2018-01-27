using AccountManagement.API;
using AccountManagement.Domain;
using Composable.Messaging.Buses;

namespace AccountManagement.UserStories.Scenarios
{
    class AccountScenarioApi
    {
        readonly IEndpoint _clientEndpoint;
        internal AccountScenarioApi(IEndpoint clientEndpoint) => _clientEndpoint = clientEndpoint;

        internal RegisterAccountScenario Register => new RegisterAccountScenario(_clientEndpoint);

        internal ChangeAccountEmailScenario ChangeEmail() => ChangeAccountEmailScenario.Create(_clientEndpoint);
        internal ChangeAccountEmailScenario ChangeEmail(AccountResource account) => new ChangeAccountEmailScenario(_clientEndpoint, account);

        internal ChangePasswordScenario ChangePassword() => ChangePasswordScenario.Create(_clientEndpoint);
        internal ChangePasswordScenario ChangePassword(AccountResource account, string oldPassword, string newPassword) => new ChangePasswordScenario(_clientEndpoint, account, oldPassword: oldPassword, newPassword: newPassword);

        internal LoginScenario Login() => LoginScenario.Create(_clientEndpoint);
        internal LoginScenario Login(RegisterAccountScenario registrationScenario) => new LoginScenario(_clientEndpoint, registrationScenario.Email, registrationScenario.Password);
        internal LoginScenario Login(Email email, string password) => new LoginScenario(_clientEndpoint, email: email.StringValue, password: password);
        internal LoginScenario Login(string email, string password) => new LoginScenario(_clientEndpoint, email: email, password: password);
    }
}
