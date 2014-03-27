using System.ComponentModel.DataAnnotations;
using AccountManagement.Domain.Shared;
using Composable.System.Linq;

namespace AccountManagement.UI.Commands.UserCommands
{
    public class PasswordAttribute : ValidationAttribute
    {        
        override public bool IsValid(object value)
        {
            if (value == null)
            {
                return true;//We validate that values are correct emails. Not that they are present. That is for the Required attribute.
            }
            return Password.Policy.GetPolicyFailures((string)value).None();
        }
    }
}