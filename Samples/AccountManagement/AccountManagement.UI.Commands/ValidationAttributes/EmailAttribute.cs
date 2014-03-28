using System.ComponentModel.DataAnnotations;
using AccountManagement.Domain.Shared;

namespace AccountManagement.UI.Commands.ValidationAttributes
{
    public class EmailAttribute : ValidationAttribute
    {
        override public bool IsValid(object value)
        {
            if(value == null)
            {
                return true; //We validate that values are correct emails. Not that they are present. That is the job of the Required attribute.
            }
            return Email.IsValidEmail((string)value);
        }
    }
}
