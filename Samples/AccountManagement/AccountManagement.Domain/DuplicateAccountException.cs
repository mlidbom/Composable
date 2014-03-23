using System;
using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain
{
    public class DuplicateAccountException : Exception
    {
        public DuplicateAccountException(Email email):base(email.ToString())
        {
        }
    }
}