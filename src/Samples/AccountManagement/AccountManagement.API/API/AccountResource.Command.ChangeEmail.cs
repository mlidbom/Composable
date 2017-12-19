using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public static partial class ChangeEmail
            {

            }
        }
    }
}