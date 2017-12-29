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

        [HttpPost]
        public async Task<IActionResult> Index(AccountResource.Command.Register.UICommand registrationCommand)
        {
            if(ModelState.IsValid)
            {
                var account = await _serviceBus.SendAsync<AccountResource>(registrationCommand);
                return View("ValidateYourEmail", account);
            }

            return View("Register");
        }

        [HttpGet]
        public async Task<IActionResult> Index() => View("Register", (await _serviceBus.QueryAsync(AccountApi.Start)).Commands.Register);
    }
}
