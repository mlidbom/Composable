using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Messaging.Commands;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public static class LogIn
            {
                internal class Domain : DomainCommand<LoginAttemptResult>
                {
                    internal Domain(Email email, string password)
                    {
                        OldContract.Argument(() => email).NotNullOrDefault();
                        OldContract.Argument(() => password).NotNullEmptyOrWhiteSpace();

                        Email = email;
                        Password = password;
                    }

                    public Email Email { get; }
                    public string Password { get; }
                }

                public class UI : DomainCommand<LoginAttemptResult>
                {
                    [Required] [Email] public string Email { get; set; }
                    [Required] public string Password { get; set; }

                    internal Domain ToDomainCommand() => new Domain(AccountManagement.Domain.Email.Parse(Email), Password);
                }
            }
        }
    }
}
