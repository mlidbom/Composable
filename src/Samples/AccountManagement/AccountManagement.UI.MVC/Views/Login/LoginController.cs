using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Login
{
    public class LoginController : Controller
    {
        readonly IServiceBus _serviceBus;
        public LoginController(IServiceBus serviceBus) => _serviceBus = serviceBus;

        public async Task<IActionResult> Login(AccountResource.Command.LogIn.UI loginCommand)
        {
            if(!ModelState.IsValid) return View("LoginForm");

            var result = await await _serviceBus.SendAsyncAsync(loginCommand);
            if(result.Succeeded)
            {
                return View("LoggedIn");
            }

            ModelState.AddModelError("Something", "Login Failed");
            return View("LoginForm");
        }

        public async Task<IActionResult> LoginForm() => View("LoginForm", (await _serviceBus.QueryAsync(AccountApi.Start)).Commands.Login);
    }
}
