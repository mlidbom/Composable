using System;
using AccountManagement.API;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Register
{
    public class RegisterController : Controller
    {
        readonly IServiceBusSession _remoteServiceBusSession;
        public RegisterController(IServiceBusSession remoteServiceBusSession) => _remoteServiceBusSession = remoteServiceBusSession;

        public IActionResult Register(AccountResource.Command.Register registrationCommand)
        {
            if(!ModelState.IsValid) return View("RegistrationForm");

            var result = _remoteServiceBusSession.PostRemote(registrationCommand);
            switch(result)
            {
                case AccountResource.Command.Register.RegistrationAttemptResult.Successful:
                    return View("ValidateYourEmail", _remoteServiceBusSession.Execute(NavigationSpecification.GetRemote(AccountApi.Start).GetRemote(start => start.Queries.AccountById.WithId(registrationCommand.AccountId))));
                case AccountResource.Command.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    ModelState.AddModelError(nameof(registrationCommand.Email), "Email is already registered");
                    return View("RegistrationForm");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IActionResult RegistrationForm() => View("RegistrationForm", (_remoteServiceBusSession.GetRemote(AccountApi.Start)).Commands.Register);
    }
}
