using System.Web.Mvc;
using AccountManagement.Web.Views.RegisterAccount;

namespace AccountManagement.Web.Controllers
{
    public class RegisterAccountController : Controller
    {
        public ViewResult DisplayAccountRegistrationView()
        {
            return View(new DisplayAccountRegistrationViewModel());
        }
    }
}