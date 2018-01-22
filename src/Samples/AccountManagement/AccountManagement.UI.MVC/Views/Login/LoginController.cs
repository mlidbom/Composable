using AccountManagement.API;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : Controller
    {
        readonly IServiceBusSession _bus;
        public LoginController(IServiceBusSession remoteServiceBusSession) => _bus = remoteServiceBusSession;

        public IActionResult Login(AccountResource.Commands.LogIn.UI loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = _bus.PostRemote(loginCommand);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            return View("LoginForm");
        }

        public IActionResult LoginForm() => View("LoginForm", (_bus.GetRemote(AccountWebClientApi.Start)).Commands.Login);
    }
}
