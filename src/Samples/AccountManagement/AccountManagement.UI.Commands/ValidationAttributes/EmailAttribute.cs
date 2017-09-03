using System.ComponentModel.DataAnnotations;
using AccountManagement.Domain;

namespace AccountManagement.UI.Commands.ValidationAttributes
{
    public class EmailAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if(string.IsNullOrEmpty((string)value))
            {
                return true; //We validate that values are correct emails. Not that they are present. That is the job of the Required attribute.
            }
            return Email.IsValidEmail((string)value);
        }
    }
}
