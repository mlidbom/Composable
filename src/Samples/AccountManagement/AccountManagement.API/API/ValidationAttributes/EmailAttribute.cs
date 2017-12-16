using AccountManagement.API.ValidationAttributes.Utilities;
using AccountManagement.Domain;

namespace AccountManagement.API.ValidationAttributes
{
    public class EmailAttribute : StringValidationAttribute
    {
        protected override bool IsValid(string value) => string.IsNullOrEmpty(value) || Email.IsValidEmail(value);
    }
}
