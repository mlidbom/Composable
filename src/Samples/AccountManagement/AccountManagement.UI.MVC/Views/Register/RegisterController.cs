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

            var result = registrationCommand.PostRemoteOn(_bus);
            switch(result)
            {
                case AccountResource.Commands.Register.RegistrationAttemptResult.Successful:
                    return View("ValidateYourEmail", Api.Accounts.Query.AccountById(registrationCommand.AccountId).ExecuteOn(_bus));
                case AccountResource.Commands.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("RegistrationForm");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IActionResult RegistrationForm() => View("RegistrationForm", Api.Accounts.Command.Register().ExecuteOn(_bus));
    }
}
