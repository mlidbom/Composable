using System.Web.Mvc;
using AccountManagement.Web.Views.Account;

namespace AccountManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        public ViewResult DisplayAccountDetails()
        {
            return View(new DisplayAccountDetailsViewModel());
        }
    }
}