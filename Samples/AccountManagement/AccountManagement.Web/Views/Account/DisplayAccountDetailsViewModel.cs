using Composable.DDD;

namespace AccountManagement.Web.Views.Account
{
    public class DisplayAccountDetailsViewModel : ValueObject<DisplayAccountDetailsViewModel>
    {
        public readonly string PageTitle = DisplayAccountDetailsViewModelResources.Title;
        public string Email { get; set; }
    }
}
