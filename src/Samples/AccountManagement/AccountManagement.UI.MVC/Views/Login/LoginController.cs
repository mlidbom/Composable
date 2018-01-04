using AccountManagement.API;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : Controller
    {
        readonly IServiceBus _serviceBus;
        public LoginController(IServiceBus serviceBus) => _serviceBus = serviceBus;

        public IActionResult Login(AccountResource.Command.LogIn.UI loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = _serviceBus.Send(loginCommand);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            return View("LoginForm");
        }

        public IActionResult LoginForm() => View("LoginForm", (_serviceBus.Query(AccountApi.Start)).Commands.Login);
    }
}
