using AccountManagement.API;
using Composable.Messaging.Hypermedia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : ControllerBase
    {
        readonly IRemoteHypermediaNavigator _bus;
        public LoginController(IRemoteHypermediaNavigator remoteApiNavigator) => _bus = remoteApiNavigator;

        public IActionResult Login(AccountResource.Command.LogIn loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = loginCommand.PostOn(_bus);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            ModelState.Remove((AccountResource.Command.LogIn model) => model.MessageId);
            loginCommand.ReplaceDeduplicationId();
            return View("LoginForm", loginCommand);
        }

        public IActionResult LoginForm() => View("LoginForm", _bus.Navigate(Api.Accounts.Command.Login()));
    }
}
