using System;
using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Register
{
    public class RegisterController : Controller
    {
        readonly IServiceBus _serviceBus;
        public RegisterController(IServiceBus serviceBus) => _serviceBus = serviceBus;

        public async Task<IActionResult> Register(AccountResource.Command.Register.UICommand registrationCommand)
        {
            if(!ModelState.IsValid) return View("RegistrationForm");

            var result = await _serviceBus.SendAsync<AccountResource.Command.Register.RegistrationAttemptResult>(registrationCommand);
            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return View("ValidateYourEmail", await _serviceBus.Get(AccountApi.Start).Get(start => start.Queries.AccountById.WithId(registrationCommand.AccountId)).ExecuteAsync());
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("RegistrationForm");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<IActionResult> RegistrationForm() => View("RegistrationForm", (await _serviceBus.QueryAsync(AccountApi.Start)).Commands.Register);
    }
}
