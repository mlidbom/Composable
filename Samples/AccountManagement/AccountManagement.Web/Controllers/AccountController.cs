using System.Web.Mvc;
using AccountManagement.Web.Views.Account;

namespace AccountManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationContext _authenticationContext;

        public AccountController(IAuthenticationContext authenticationContext)
        {
            _authenticationContext = authenticationContext;
        }

        public ViewResult DisplayAccountDetails()
        {
            return View(new DisplayAccountDetailsViewModel());
        }
    }
}