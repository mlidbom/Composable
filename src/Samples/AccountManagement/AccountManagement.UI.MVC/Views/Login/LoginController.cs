using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : ControllerBase
    {
        readonly IRemoteApiBrowserSession _bus;
        public LoginController(IRemoteApiBrowserSession remoteApiBrowser) => _bus = remoteApiBrowser;

        public IActionResult Login(AccountResource.Command.LogIn loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = loginCommand.PostOn(_bus);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            return View("LoginForm");
        }

        public IActionResult LoginForm() => View("LoginForm", Api.Accounts.Command.Login().NavigateOn(_bus));
    }
}
