using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public partial class LogIn : BusApi.RemoteSupport.AtMostOnce.Command<LogIn.LoginAttemptResult>
            {
                    [Required] [Email] public string Email { get; set; }
                    [Required] public string Password { get; set; }

                    internal LogIn WithValues(string email, string password) => new LogIn
                                                                           {
                                                                               Email = email,
                                                                               Password = password
                                                                           };
            }
        }
    }
}
