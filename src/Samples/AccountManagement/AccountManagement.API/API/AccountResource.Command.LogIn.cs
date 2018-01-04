using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable;
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
                [TypeId("14B6DD28-205B-42ED-9CF4-20D6B0299632")]internal class Domain : TransactionalExactlyOnceDeliveryCommand<LoginAttemptResult>
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

                [TypeId("FD3A793F-CEDE-4082-B710-1C143133C9E5")]public class UI : TransactionalExactlyOnceDeliveryCommand<LoginAttemptResult>
                {
                    [Required] [Email] public string Email { get; set; }
                    [Required] public string Password { get; set; }

                    internal Domain ToDomainCommand() => new Domain(AccountManagement.Domain.Email.Parse(Email), Password);
                }
            }
        }
    }
}
