using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.UI.Commands.ValidationAttributes;
using Composable.DDD;

namespace AccountManagement.UI.Commands.UserCommands
{
    public class RegisterAccountCommand : ValueObject<RegisterAccountCommand>
    {
        //Note the use of a custom validation attribute.
        [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")]
        public Guid AccountId { get; set; }

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