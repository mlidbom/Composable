using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain
{
    public class DuplicateAccountException : Exception
    {
        internal DuplicateAccountException(Email email) : base(email.ToString()) {}
    }
}
