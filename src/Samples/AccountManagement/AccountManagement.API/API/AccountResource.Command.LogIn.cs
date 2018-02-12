using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging;
using Newtonsoft.Json;

// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public partial class LogIn : BusApi.Remotable.AtMostOnce.Command<LogIn.LoginAttemptResult>
            {
                public LogIn() : base(MessageIdHandling.Reuse) {}

                public static LogIn Create() => new LogIn {DeduplicationId = Guid.NewGuid()};

                [Required] [Email] public string Email { get; set; }
                [Required] public string Password { get; set; }

                internal LogIn WithValues(string email, string password) => new LogIn
                                                                            {
                                                                                DeduplicationId = DeduplicationId,
                                                                                Email = email,
                                                                                Password = password
                                                                            };
            }
        }
    }
}
