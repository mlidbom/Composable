using System.Diagnostics;
using AccountManagement.UI.MVC.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccountManagement.UI.MVC.Views.Home
{
    public class HomeController : Controller
    {
        public IActionResult Index() =>
            View();

        public IActionResult Error() =>
            View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
    }
}
