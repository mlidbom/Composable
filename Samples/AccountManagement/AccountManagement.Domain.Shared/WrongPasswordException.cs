using System;

namespace AccountManagement.Domain.Shared
{
    ///<summary>Thrown if an attempt is made to authenticate with a password that does not match the password for the account.</summary>
    class WrongPasswordException : Exception {}
}
