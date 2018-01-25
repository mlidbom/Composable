using System;
using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Register
{
    public class RegisterController : ControllerBase
    {
        readonly IServiceBusSession _bus;
        public RegisterController(IServiceBusSession remoteServiceBusSession) => _bus = remoteServiceBusSession;

        public IActionResult Register(AccountResource.Commands.Register registrationCommand)
        {
            if(!ModelState.IsValid) return View("RegistrationForm");

            var result = registrationCommand.PostOn(_bus);
            switch(result.Status)
            {
                case AccountResource.Commands.Register.RegistrationAttemptResult.Statuses.Successful:
                    return View("ValidateYourEmail", result.RegisteredAccount);
                case AccountResource.Commands.Register.RegistrationAttemptResult.Statuses.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("RegistrationForm");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IActionResult RegistrationForm() => View("RegistrationForm", Api.Accounts.Command.Register().ExecuteOn(_bus));
    }
}
