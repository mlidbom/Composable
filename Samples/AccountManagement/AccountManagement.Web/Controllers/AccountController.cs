using System.Web.Mvc;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.Web.Views.Account;

namespace AccountManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationContext _authenticationContext;
        private readonly IAccountManagementQueryModelSession _queryModels;

        public AccountController(IAuthenticationContext authenticationContext, IAccountManagementQueryModelSession queryModels)
        {
            _authenticationContext = authenticationContext;
            _queryModels = queryModels;
        }

        public ViewResult DisplayAccountDetails()
        {
            return View(new DisplayAccountDetailsViewModel()
                        {
                            Email = _queryModels.Get<AccountQueryModel>(_authenticationContext.AccountId).Email.ToString()
                        });
        }
    }
}