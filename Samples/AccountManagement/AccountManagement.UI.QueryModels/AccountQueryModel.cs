using AccountManagement.Domain.Shared;
using Composable.DDD;

namespace AccountManagement.UI.QueryModels
{
    public class AccountQueryModel : ValueObject<AccountQueryModel>
    {
        public Password Password { get; set; }
        public Email Email { get; set; }
    }
}