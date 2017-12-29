using System;
using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Register
{
    public class RegisterController : Controller
    {
        readonly IServiceBus _serviceBus;
        public RegisterController(IServiceBus serviceBus) => _serviceBus = serviceBus;

        public async Task<IActionResult> Register(AccountResource.Command.Register.UICommand registrationCommand)
        {
            if(!ModelState.IsValid) return View("Register");

            var result = await _serviceBus.SendAsync<AccountResource.Command.Register.RegistrationAttemptResult>(registrationCommand);
            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return View("ValidateYourEmail", await _serviceBus.Get(AccountApi.Start).Get(start => start.Queries.AccountById.Mutate(@this => @this.Id = registrationCommand.AccountId)).ExecuteAsync());
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("Register");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<IActionResult> Index() => View("Register", (await _serviceBus.QueryAsync(AccountApi.Start)).Commands.Register);
    }
}
