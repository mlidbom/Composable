using Composable.DDD;

namespace AccountManagement.Web.Views.RegisterAccount
{
    public class DisplayAccountRegistrationViewModel : ValueObject<DisplayAccountRegistrationViewModel>
    {
        public readonly string PageTitle =  DisplayAccountRegistrationViewModelResources.Title;
    }
}