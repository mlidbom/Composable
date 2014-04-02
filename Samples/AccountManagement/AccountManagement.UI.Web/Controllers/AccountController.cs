using System.Web.Mvc;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.Web.Views.Account;

namespace AccountManagement.UI.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationContext _authenticationContext;
        private readonly IAccountManagementQueryModelsReader _queryModels;

        public AccountController(IAuthenticationContext authenticationContext, IAccountManagementQueryModelsReader queryModels)
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
