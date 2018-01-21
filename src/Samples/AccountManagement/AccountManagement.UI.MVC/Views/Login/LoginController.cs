using AccountManagement.API;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : Controller
    {
        readonly IRemoteServiceBusSession _remoteServiceBusSession;
        public LoginController(IRemoteServiceBusSession remoteServiceBusSession) => _remoteServiceBusSession = remoteServiceBusSession;

        public IActionResult Login(AccountResource.Command.LogIn.UI loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = _remoteServiceBusSession.PostRemote(loginCommand);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            return View("LoginForm");
        }

        public IActionResult LoginForm() => View("LoginForm", (_remoteServiceBusSession.GetRemote(AccountApi.Start)).Commands.Login);
    }
}
