using System.ComponentModel.DataAnnotations;
using Composable.DDD;

namespace AccountManagement.UI.Commands.UserCommands
{
    public class RegisterAccountCommand : ValueObject<RegisterAccountCommand>
    {
        //Note the use of a custom validation attribute.
        [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")]        
        public string Email { get; set; }


        //Note the use of a custom validation attribute.
        [Password(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordInvalid")]
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")]
        public string Password { get; set; }
    }
}